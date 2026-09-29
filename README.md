# ExpenseHub — Checkpoint 2 C# Software Development

API REST desenvolvida em ASP.NET Core para gerenciamento de solicitações corporativas de reembolso.

O sistema permite o cadastro e autenticação de usuários, controle de acesso por roles, criação e acompanhamento de despesas, aprovação, reprovação, pagamento e histórico das operações.

## Integrantes

- Luan Ramos Garcia de Souza — RM558537
- Matheus Bortolotto — RM555189
- Matheus Ricciotti — RM556930

## Tecnologias

- .NET 10
- ASP.NET Core
- ASP.NET Core Identity
- Entity Framework Core
- SQLite
- JWT Bearer Authentication
- MSTest

## Estrutura

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/
│   ├── Auth/
│   ├── Data/
│   ├── Domain/
│   │   ├── Entities/
│   │   └── Enums/
│   ├── Identity/
│   ├── Migrations/
│   ├── Program.cs
│   └── appsettings.json
│
└── ExpenseHub.UnitTests/
```

## Requisitos

Para executar o projeto é necessário possuir:

- .NET SDK 10
- Entity Framework Core CLI (`dotnet-ef`)

Para verificar a versão instalada:

```powershell
dotnet --version
```

Para instalar o `dotnet-ef`:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

## Restaurar dependências

A partir da pasta `sources`:

```powershell
dotnet restore .\ExpenseHub.slnx
```

## Banco de dados

O projeto utiliza SQLite como banco de dados relacional por meio do Entity Framework Core.

### Provider

Pacotes utilizados:

- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Design`

### Connection string

A configuração está disponível em:

```text
ExpenseHub.Api/appsettings.json
```

Configuração:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=expensehub.db"
}
```

O arquivo local `expensehub.db` não é versionado no Git.

### Criar ou atualizar o banco

A partir da pasta `sources`:

```powershell
dotnet ef database update `
  --project .\ExpenseHub.Api\ExpenseHub.Api.csproj `
  --startup-project .\ExpenseHub.Api\ExpenseHub.Api.csproj
```

### Criar uma nova migration

Quando houver alteração no modelo:

```powershell
dotnet ef migrations add NomeDaMigration `
  --project .\ExpenseHub.Api\ExpenseHub.Api.csproj `
  --startup-project .\ExpenseHub.Api\ExpenseHub.Api.csproj
```

> `NomeDaMigration` deve ser substituído pelo nome real da migration.

## Autenticação

A aplicação utiliza ASP.NET Core Identity com autenticação JWT Bearer.

### Roles

As seguintes roles são criadas automaticamente:

- `Admin`
- `Employee`
- `Approver`
- `Finance`
- `Auditor`

O seed cria somente uma conta inicial e atribui a ela a role `Admin`.

O seed é idempotente, portanto executar a aplicação novamente não deve duplicar usuários ou roles.

## Configuração do Admin

As credenciais do Admin não são armazenadas no código-fonte ou no `appsettings.json`.

O projeto utiliza .NET User Secrets.

Inicialize os User Secrets, caso necessário:

```powershell
dotnet user-secrets init --project .\ExpenseHub.Api\ExpenseHub.Api.csproj
```

Configure o e-mail do Admin:

```powershell
dotnet user-secrets set "AdminSeed:Email" "admin@expensehub.local" --project .\ExpenseHub.Api\ExpenseHub.Api.csproj
```

Configure uma senha segura:

```powershell
dotnet user-secrets set "AdminSeed:Password" "SUA-SENHA-SEGURA" --project .\ExpenseHub.Api\ExpenseHub.Api.csproj
```

## Configuração JWT

As configurações públicas do JWT estão em `appsettings.json`.

Exemplo:

```json
"Jwt": {
  "Issuer": "ExpenseHub.Api",
  "Audience": "ExpenseHub.Client",
  "ExpirationMinutes": 60
}
```

A chave utilizada para assinatura do token deve ser configurada por User Secrets:

```powershell
dotnet user-secrets set "Jwt:Key" "SUA-CHAVE-JWT-SEGURA" --project .\ExpenseHub.Api\ExpenseHub.Api.csproj
```

Nunca versione a chave JWT real.

## Executar a aplicação

A partir da pasta `sources`:

```powershell
dotnet run --project .\ExpenseHub.Api\ExpenseHub.Api.csproj
```

Em ambiente local a API poderá iniciar, por exemplo, em:

```text
http://localhost:5245
```

A porta pode variar conforme o `launchSettings.json`.

## Health Check

Endpoint:

```http
GET /health
```

Resposta esperada:

```json
{
  "status": "ok"
}
```

## Login

Endpoint:

```http
POST /login
```

Exemplo de requisição:

```json
{
  "email": "admin@expensehub.local",
  "password": "SUA-SENHA-SEGURA"
}
```

Credenciais válidas retornam:

- token JWT Bearer;
- data de expiração;
- roles do usuário.

Exemplo de resposta:

```json
{
  "accessToken": "TOKEN_JWT",
  "expiresAtUtc": "2026-10-01T00:00:00Z",
  "roles": [
    "Admin"
  ]
}
```

Credenciais inválidas retornam:

```text
401 Unauthorized
```

Após qualquer alteração de roles, o usuário deve autenticar novamente para receber um novo token com as permissões atualizadas.

## Modelo de domínio

As entidades mínimas utilizadas pelo sistema são:

- `Expense`
- `ExpenseCategory`
- `ExpenseHistory`
- `PaymentRecord`

Estados possíveis de uma despesa:

```text
Draft
Submitted
Approved
Rejected
Paid
```

## Fluxo de reembolso

Fluxo principal previsto:

```text
Draft
  ↓
Submitted
  ├── Approved
  │      ↓
  │     Paid
  │
  └── Rejected
```

`Rejected` e `Paid` são estados finais.

## Build

A partir da pasta `sources`:

```powershell
dotnet build .\ExpenseHub.slnx
```

O projeto deve compilar sem erros e sem warnings.

## Testes

Para executar os testes:

```powershell
dotnet test .\ExpenseHub.slnx
```

Os testes unitários do projeto ficam em:

```text
ExpenseHub.UnitTests
```

Os testes devem executar sem depender de:

- banco de dados externo;
- rede;
- serviços externos.

## Validação completa

Antes da entrega:

```powershell
dotnet restore .\ExpenseHub.slnx
dotnet build .\ExpenseHub.slnx
dotnet test .\ExpenseHub.slnx
```

Também deve ser verificado o workflow `code-quality` no GitHub Actions.

## Segurança

Não devem ser versionados:

- senhas;
- tokens;
- chaves JWT;
- secrets;
- bancos SQLite locais;
- arquivos `.env`;
- `appsettings.Development.json`;
- `appsettings.Local.json`.

Credenciais locais devem ser configuradas utilizando User Secrets.

## Git

O desenvolvimento é realizado utilizando branches individuais e Pull Requests para integração com a `main`.

Todos os integrantes devem possuir múltiplos commits próprios representando contribuições reais ao projeto.

As issues do checkpoint original são utilizadas como referência para implementação das funcionalidades.

## Projeto acadêmico

Projeto desenvolvido para o Checkpoint 2 da disciplina de Software Development C#.
