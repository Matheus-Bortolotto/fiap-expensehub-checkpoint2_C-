# Contexto de integração I06 a I08

Este arquivo registra a parte já preparada na branch `Luan`. A `main` usada como base ainda contém somente I01 e I02. Portanto, não houve alteração nos endpoints que pertencem às I03, I04 e I05.

## O que foi adicionado

- `Expenses/ExpenseAccessService.cs`: aplica a visibilidade por role diretamente em `IQueryable<Expense>`. Deve ser usado pela listagem e detalhe da I05 para impedir materialização de despesas fora do escopo.
- `Expenses/ExpenseWorkflowService.cs`: centraliza as transições contextuais de I07 e I08. Ele bloqueia autoaprovação e autopagamento, devolve resultado `NotFound`, `Forbidden` ou `Conflict`, e persiste cada mudança com o respectivo histórico na mesma chamada de `SaveChangesAsync`.
- `Expenses/ExpenseWorkflowEndpoints.cs`: mapeia `POST /api/expenses/{id}/approve`, `reject`, `pay` e `GET /api/expenses/{id}/history`.
- `Expenses/RejectExpenseRequest.cs`: valida justificativa de reprovação entre 10 e 500 caracteres.
- `Expenses/ExpenseHistoryResponse.cs`: representa a resposta de histórico sem expor a entidade diretamente.

## Integração obrigatória com I04 e I05

Ao implementar I04/I05, reutilize os componentes acima e não replique regras em outro `ExpenseService`.

1. Na criação do Draft, defina `OwnerId` a partir do JWT, `Status = Draft` e chame `ExpenseWorkflowService.AddHistory` com ação `Created`, status anterior nulo e status novo `Draft`. Salve a despesa e o histórico juntos.
2. Na edição de Draft, depois da checagem de proprietário e estado, chame `AddHistory` com ação `Updated`, status anterior e novo `Draft` e uma descrição objetiva das mudanças no argumento `changes`.
3. No submit, permita somente `Draft -> Submitted` para o proprietário. Antes do mesmo `SaveChangesAsync`, chame `AddHistory` com ação `Submitted`, estado anterior `Draft` e novo `Submitted`.
4. Na listagem e detalhe, inicie a consulta com `ExpenseAccessService.ApplyVisibility(context.Expenses, user)`. Para detalhe invisível ou inexistente, devolva `404`.
5. Não dê acesso funcional somente por `Admin`. A role Admin continua necessária apenas em administração de usuários. Roles acumuladas somam visibilidade, porém o `ExpenseWorkflowService` mantém a proibição de autoaprovação/autopagamento.

## Endpoints já preparados

| Rota | Regra aplicada |
| --- | --- |
| `POST /api/expenses/{id}/approve` | Approver não proprietário, `Submitted -> Approved` |
| `POST /api/expenses/{id}/reject` | Approver não proprietário, justificativa 10–500, `Submitted -> Rejected` |
| `POST /api/expenses/{id}/pay` | Finance não proprietário, `Approved -> Paid`, cria `PaymentRecord` |
| `GET /api/expenses/{id}/history` | Aplica a mesma visibilidade de leitura da despesa |

Os endpoints exigem JWT. Ausência ou invalidez de token permanece responsabilidade do middleware de autenticação (`401`). A camada de workflow retorna `403` para role insuficiente ou operação na própria despesa, `404` para despesa inexistente e `409` para estado incompatível ou repetição.

## Pontos ainda pendentes

- Adicionar testes de integração/unitários para aprovação, reprovação, pagamento e histórico usando um provider de teste. Nesta branch foram adicionados testes sem banco para a matriz de leitura.
- Executar `dotnet restore`, `dotnet build` e `dotnet test` com o SDK .NET 10 antes de abrir PR. Esta máquina não possui o SDK instalado no PATH.
