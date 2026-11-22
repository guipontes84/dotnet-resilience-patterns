# .NET Resilience Patterns

**Repositório:** https://github.com/guipontes84/dotnet-resilience-patterns

Exemplo didático de **Retry, Timeout e Circuit Breaker** com **C# / ASP.NET Core 8**, `HttpClientFactory` e **Polly** via `Microsoft.Extensions.Http.Resilience`.

## Executar

Requer .NET SDK 8 ou superior.

```bash
dotnet restore src/ResilienceDemo/ResilienceDemo.csproj
dotnet run --project src/ResilienceDemo/ResilienceDemo.csproj
```

Em outro terminal:

```bash
curl "http://localhost:5080/demo?mode=ok"
curl "http://localhost:5080/demo?mode=flaky"
curl "http://localhost:5080/demo?mode=slow"
curl "http://localhost:5080/demo?mode=down"
```

## Cenários

| Modo | Comportamento |
| --- | --- |
| `ok` | Retorna sucesso imediatamente |
| `flaky` | Falha com HTTP 503 em duas de cada três chamadas ao simulador |
| `slow` | Demora 5 segundos, excedendo o timeout de 3 segundos por tentativa |
| `down` | Retorna HTTP 503 continuamente, permitindo observar o Circuit Breaker |

## Configurações

- **Retry:** até 3 novas tentativas, backoff exponencial e jitter.
- **Timeout:** 3 segundos por tentativa e 20 segundos no total.
- **Circuit Breaker:** janela de 20 segundos, mínimo de 5 amostras, taxa de falhas de 50% e abertura por 15 segundos.

> **Observação:** é um laboratório de demonstração, não um template de produção. O endereço local está fixo em `http://localhost:5080`. O contador do simulador é compartilhado entre chamadas. Não aplique retries a operações não idempotentes sem uma estratégia de idempotência.

## Estrutura

```text
src/ResilienceDemo/
  ResilienceDemo.csproj
  Program.cs
.gitignore
README.md
```

## Próximos passos

Adicionar testes automatizados, telemetria com OpenTelemetry, configuração por ambiente e um serviço externo separado para cenários mais realistas.
