# financial-lab

Laboratório de transferências entre contas com serviços .NET 9 que conversam por mensageria, mais um antifraude externo simulado que responde por webhook.

| Serviço | Porta | Responsabilidade |
|---|---|---|
| Account API | `5157` | Saldo e ledger das contas. Consome `DebitAccount` / `CreditAccount` e publica `AccountDebited`, `AccountCredited` ou as recusas. |
| Transfer API | `5257` | Recebe transferências e simulações, orquestra antifraude → débito → crédito → estorno. |
| Antifraud API | `5357` | Simulador de um antifraude externo: analisa cada transferência por regras configuráveis e devolve a decisão por webhook assinado. Estado em memória. |
| PostgreSQL | `5433` | Bancos `financial_lab_account` e `financial_lab_transfer` (usuário/senha `postgres`). |
| RabbitMQ | `5672` / `15672` | Broker. Painel em http://localhost:15672 (`guest` / `guest`). |

Fluxo de uma transferência: `POST /transfers` responde `202` com status `Screening` → o antifraude analisa e chama `POST /webhooks/antifraud` → se recusar, a transferência fica `FraudRejected`; se aprovar, vai para `Debiting` → a Account API debita a origem → a Transfer API pede o crédito no destino → `Completed`. Se o débito for recusado, a transferência fica `Failed`. Se o crédito for recusado, o valor é devolvido à origem e ela fica `Refunded`.

## Antifraude simulado

A Transfer API envia cada transferência para `POST /analyses` do antifraude (via outbox, com retry). O antifraude decide pela primeira regra habilitada que casar (sem regra, aprova), espera o atraso configurado e chama o callback com os headers `X-Antifraud-Delivery`, `X-Antifraud-Timestamp` e `X-Antifraud-Signature: sha256=HMAC(segredo, "<timestamp>.<corpo>")`. A Transfer API recusa assinatura inválida ou com mais de 5 min com `401`, e webhooks repetidos ou fora de ordem não mudam uma decisão já aplicada.

Se o webhook não chegar, a Transfer API reconcilia: transferências em `Screening` há mais de `StaleAfterSeconds` são consultadas em `GET /analyses?transactionId=<endToEndId>`, e depois de `TimeoutSeconds` sem decisão ficam `FraudRejected` com "O antifraude não respondeu a tempo." (fail-closed).

### Regras

As regras iniciais ficam em `financial-lab-antifraud.Api/appsettings.json` (`Antifraud:Rules`) e podem ser trocadas em tempo de execução:

| Método | Rota | Efeito |
|---|---|---|
| `GET` | `/rules` | Lista as regras, na ordem de avaliação |
| `POST` | `/rules` | Adiciona uma regra no fim (`409` se o id existir) |
| `PUT` | `/rules/{id}` | Cria ou substitui uma regra |
| `PUT` | `/rules` | Substitui a lista inteira |
| `DELETE` | `/rules/{id}` | Remove uma regra |
| `POST` | `/rules/reset` | Volta às regras do `appsettings.json` |

Campos de uma regra (todas as condições informadas precisam casar):

| Campo | Tipo | Descrição |
|---|---|---|
| `id` | string | Obrigatório, único |
| `enabled` | bool | Padrão `true` |
| `decision` | `Approved` \| `Rejected` | Padrão `Rejected` |
| `reason` | string | Vai para o `failureReason` da transferência |
| `minAmount` / `maxAmount` | decimal | Faixa de valor, inclusiva |
| `sourceAccountIds` / `destinationAccountIds` | guid[] | Casa se a conta estiver na lista |
| `descriptionContains` | string | Sem diferenciar maiúsculas |
| `delayMs` | int | Atraso até a decisão (padrão `DefaultDelayMs`, 300) |
| `delivery` | `Normal` \| `Duplicate` \| `Drop` \| `InvalidSignature` \| `Flip` | Como o webhook é entregue: normal, duas vezes, nunca, com assinatura errada, ou a decisão seguida da oposta |
| `submitFailures` | int | Responde `503` às primeiras N submissões da transação |

Regras padrão: `valor-alto` recusa valores a partir de R$ 1.000,00; `destino-bloqueado` (desabilitada) recusa transferências para a conta 3.

```sh
curl -X POST http://localhost:5357/rules \
  -H 'Content-Type: application/json' \
  -d '{"id":"golpe","decision":"Rejected","reason":"Descrição suspeita.","descriptionContains":"golpe"}'

curl 'http://localhost:5357/analyses?transactionId=<endToEndId>'
```

`GET /analyses/{id}` e `GET /analyses?transactionId=` mostram a decisão, a regra que casou e cada tentativa de entrega do webhook. `DELETE /analyses` limpa as análises em memória.

## Rodando tudo com Docker

### Pré-requisitos

- Docker Engine com os plugins **Compose v2** e **Buildx**. Confira com:

  ```sh
  docker compose version
  docker buildx version
  ```

  Se algum faltar no Ubuntu:

  ```sh
  sudo apt install docker-compose-v2 docker-buildx
  ```

Não é preciso ter o .NET instalado: a compilação acontece dentro da imagem.

### Subir

```sh
docker compose up -d --build
```

O Compose sobe, nesta ordem:

1. `postgres` e `rabbitmq`, aguardando o healthcheck de cada um;
2. `migrations`, que aplica as migrations dos dois bancos e termina (cria também as 3 contas de teste);
3. `antifraud-api`, que não depende de banco nem de fila;
4. `account-api` e `transfer-api`, só depois que as migrations terminarem com sucesso.

Acompanhe com:

```sh
docker compose ps
docker compose logs -f account-api transfer-api antifraud-api
```

O serviço `migrations` aparece como `Exited (0)` em `docker compose ps -a`, e isso é esperado.

### Testar

As contas de teste já começam com R$ 30,00:

| Conta | Id |
|---|---|
| 1 | `11111111-1111-1111-1111-111111111111` |
| 2 | `22222222-2222-2222-2222-222222222222` |
| 3 | `33333333-3333-3333-3333-333333333333` |

```sh
curl http://localhost:5157/accounts/11111111-1111-1111-1111-111111111111/balance

curl -X POST http://localhost:5257/transfers \
  -H 'Content-Type: application/json' \
  -d '{"sourceAccountId":"11111111-1111-1111-1111-111111111111","destinationAccountId":"22222222-2222-2222-2222-222222222222","amount":10.00,"description":"Pix de teste"}'

curl http://localhost:5257/transfers/<id-devolvido-no-post>

curl -X POST http://localhost:5257/simulations \
  -H 'Content-Type: application/json' \
  -d '{"transfers":50,"minAmount":1.00,"maxAmount":20.00}'

curl http://localhost:5257/simulations/<simulationId>
```

Os arquivos `.http` de cada API (`financial-lab-account.Api`, `financial-lab-transfer.Api` e `financial-lab-antifraud.Api`) têm as mesmas requisições prontas para o Rider/VS Code, e funcionam igual com a stack no Docker.

O OpenAPI fica em http://localhost:5157/openapi/v1.json, http://localhost:5257/openapi/v1.json e http://localhost:5357/openapi/v1.json.

Para consultar o banco direto:

```sh
docker exec -it financial-lab-postgres psql -U postgres -d financial_lab_account
docker exec -it financial-lab-postgres psql -U postgres -d financial_lab_transfer
```

### Parar e resetar

```sh
docker compose down
docker compose down -v
```

O primeiro comando para tudo e mantém os dados. O segundo também apaga os volumes: na próxima subida, os bancos são recriados e as contas voltam a R$ 30,00.

Depois de alterar o código, rode `docker compose up -d --build` de novo para reconstruir as imagens.

## Rodando as APIs fora do Docker

Útil para depurar no Rider. Suba só a infraestrutura e rode as APIs com o SDK local:

```sh
docker compose up -d --wait postgres rabbitmq

dotnet tool install --global dotnet-ef --version 9.0.9
dotnet ef database update -p financial-lab-account.Data  -s financial-lab-account.Api
dotnet ef database update -p financial-lab-transfer.Data -s financial-lab-transfer.Api

dotnet run --project financial-lab-account.Api  --launch-profile http
dotnet run --project financial-lab-transfer.Api --launch-profile http
dotnet run --project financial-lab-antifraud.Api --launch-profile http
```

Rode cada `dotnet run` em um terminal. As portas são as mesmas do Docker, então pare os containers das APIs antes (`docker compose stop account-api transfer-api antifraud-api`).

O `global.json` exige o SDK **9.0.x**. Se o `dotnet --version` mostrar outra versão, o SDK 9 pode estar instalado em `~/.dotnet` e não estar no `PATH`. Corrija com:

```sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
```
