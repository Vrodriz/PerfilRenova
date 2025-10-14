# PerfilRenova API

API REST para gerenciamento de clientes e assinaturas do sistema PerfilRenova. Construída com ASP.NET Core 9.0, Entity Framework Core e autenticação JWT.

## Índice

- [Tecnologias](#tecnologias)
- [Pré-requisitos](#pré-requisitos)
- [Instalação](#instalação)
- [Configuração](#configuração)
- [Executando o Projeto](#executando-o-projeto)
- [Documentação da API](#documentação-da-api)
- [Endpoints Disponíveis](#endpoints-disponíveis)
- [Autenticação](#autenticação)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Contribuindo](#contribuindo)

## Tecnologias

- **ASP.NET Core 9.0** - Framework web
- **Entity Framework Core 9.0** - ORM para acesso ao banco de dados
- **MySQL** - Banco de dados relacional (via Pomelo.EntityFrameworkCore.MySql)
- **JWT (JSON Web Tokens)** - Autenticação e autorização
- **Swagger/OpenAPI** - Documentação interativa da API
- **BCrypt.Net** - Hash seguro de senhas

## Pré-requisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MySQL Server 8.0+](https://dev.mysql.com/downloads/mysql/)
- IDE de sua preferência (Visual Studio, VS Code, Rider)

## Instalação

1. Clone o repositório:
```bash
git clone <url-do-repositorio>
cd Perfil-Renova
```

2. Restaure as dependências:
```bash
cd PerfilRenovaWeb.api
dotnet restore
```

3. Configure a string de conexão do banco de dados (veja [Configuração](#configuração))

4. Execute as migrações do banco de dados:
```bash
dotnet ef database update
```

## Configuração

### Arquivo appsettings.json

Crie ou edite o arquivo `appsettings.json` na raiz do projeto:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=perfilrenova;User=seu_usuario;Password=sua_senha;"
  },
  "Jwt": {
    "Key": "sua_chave_secreta_com_pelo_menos_16_caracteres",
    "Issuer": "PerfilRenovaAPI",
    "Audience": "PerfilRenovaApp",
    "ExpirationMinutes": 120
  }
}
```

### Variáveis de Ambiente (Produção)

Para ambientes de produção, é recomendado usar variáveis de ambiente:

```bash
export ConnectionStrings__DefaultConnection="Server=..."
export Jwt__Key="sua_chave_secreta_segura"
```

## Executando o Projeto

### Modo Desenvolvimento

```bash
dotnet run
```

A API estará disponível em:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

### Modo Produção

```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet PerfilRenovaWeb.api.dll
```

## Documentação da API

A documentação interativa Swagger está disponível em:
- **Local:** `http://localhost:5000` (redirecionado automaticamente para Swagger UI)
- **Swagger JSON:** `http://localhost:5000/swagger/v1/swagger.json`

A interface Swagger permite:
- Visualizar todos os endpoints disponíveis
- Testar requisições diretamente no navegador
- Ver exemplos de requisição/resposta
- Autenticar usando tokens JWT

## Endpoints Disponíveis

### Autenticação

#### `POST /api/auth/login`
Autentica um usuário e retorna um token JWT.

**Requisição:**
```json
{
  "username": "admin",
  "password": "sua_senha"
}
```

**Resposta (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "username": "admin",
  "role": "Admin",
  "expiresAt": "2025-01-15T14:30:00Z"
}
```

#### `POST /api/auth/seed-initial-user`
Cria o usuário administrador inicial (usar apenas uma vez).

> **⚠️ ATENÇÃO:** Este endpoint deve ser removido após criar o primeiro usuário!

### Clientes

Todos os endpoints de clientes requerem autenticação via token JWT.

#### `GET /api/clientes`
Lista clientes com paginação e filtros.

**Parâmetros de Query:**
- `search` (string, opcional): Busca por CNPJ/CPF ou descrição
- `status` (string, opcional): Filtro de status
  - `ativos`: Clientes não bloqueados com assinatura válida
  - `bloqueado`: Clientes bloqueados
  - `vencidos`: Clientes com assinatura vencida
  - `próximos a vencer`: Assinatura válida nos próximos 15 dias
- `page` (int, padrão: 1): Número da página
- `pageSize` (int, padrão: 10): Itens por página

**Exemplo:**
```
GET /api/clientes?search=12345&status=ativos&page=1&pageSize=10
```

#### `PATCH /api/clientes/{id}`
Atualiza um cliente específico.

**Requisição:**
```json
{
  "bloqueado": "S",
  "dataValidade": "2025-12-31",
  "pendente": false
}
```

#### `PATCH /api/clientes/bulk`
Atualiza múltiplos clientes em lote.

**Requisição:**
```json
{
  "clientIds": [1, 2, 3],
  "bloqueado": "N",
  "dataValidade": "2025-12-31",
  "pendente": false
}
```

#### `POST /api/clientes/{id}/block`
Bloqueia um cliente.

**Parâmetros de Query:**
- `reason` (string, opcional): Motivo do bloqueio
  - `expired`: Assinatura expirada
  - `payment`: Pagamento pendente
  - Outros: Bloqueado manualmente

#### `POST /api/clientes/{id}/unblock`
Desbloqueia um cliente (apenas se a assinatura estiver válida).

#### `POST /api/clientes/{id}/renew`
Renova a assinatura de um cliente.

**Requisição:**
```json
{
  "dataValidade": "2026-01-15"
}
```

## Autenticação

A API utiliza autenticação baseada em JWT (JSON Web Tokens).

### Como Autenticar

1. Faça login no endpoint `/api/auth/login` com suas credenciais
2. Copie o token retornado
3. Inclua o token no header `Authorization` das próximas requisições:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### Usando o Swagger UI

1. Clique no botão **Authorize** no topo da página
2. Digite: `Bearer {seu_token}` (substitua `{seu_token}` pelo token recebido)
3. Clique em **Authorize**
4. Todas as requisições seguintes incluirão automaticamente o token

### Token Expirado

Os tokens expiram após 2 horas (configurável). Quando isso acontecer:
- Você receberá um erro `401 Unauthorized`
- Faça login novamente para obter um novo token

## Estrutura do Projeto

```
PerfilRenovaWeb.api/
├── controllers/
│   ├── AuthController.cs       # Autenticação e login
│   └── ClientesController.cs   # Gerenciamento de clientes
├── Data/
│   └── ApplicationDbContext.cs # Contexto do EF Core
├── DTOs/
│   ├── AuthDTO.cs              # DTOs de autenticação
│   └── ClientDtos.cs           # DTOs de clientes
├── models/
│   ├── Client.cs               # Modelo de cliente
│   ├── ClientMessage.cs        # Mensagens de cliente
│   └── Usuario.cs              # Modelo de usuário
├── Program.cs                  # Configuração da aplicação
├── appsettings.json           # Configurações
└── PerfilRenovaWeb.api.csproj # Arquivo de projeto
```

## Segurança

### Boas Práticas Implementadas

- Senhas armazenadas com hash BCrypt
- Autenticação JWT com chaves simétricas
- Validação de issuer e audience
- CORS configurado para origens específicas
- Retry automático em falhas de conexão com o banco

### Configurações de Segurança Recomendadas

1. **Produção:**
   - Use HTTPS obrigatório
   - Configure `RequireHttpsMetadata = true` no JWT
   - Use variáveis de ambiente para secrets
   - Limite as origens CORS

2. **Chave JWT:**
   - Mínimo de 16 caracteres
   - Use chaves aleatórias e seguras
   - Nunca commite chaves no controle de versão

3. **Banco de Dados:**
   - Use usuário específico com permissões limitadas
   - Mantenha backups regulares
   - Configure SSL/TLS para conexões

## Primeiro Uso

### Criando o Usuário Administrador

1. Inicie a aplicação
2. Acesse o Swagger UI
3. Execute o endpoint `POST /api/auth/seed-initial-user`
4. **Credenciais criadas:**
   - Username: `admin`
   - Password: `adm123`
5. **⚠️ IMPORTANTE:** Troque a senha imediatamente!
6. **⚠️ REMOVA** o endpoint `seed-initial-user` do código após criar o usuário

## CORS

A API está configurada para aceitar requisições das seguintes origens:
- `http://localhost:5173` (Vite)
- `http://localhost:3000` (React/Next.js)
- `http://localhost:5174`

Para adicionar mais origens, edite a seção CORS em [Program.cs:70-83](PerfilRenovaWeb.api/Program.cs#L70-L83).

## Troubleshooting

### Erro de Conexão com o Banco

```
Connection string não configurada
```
**Solução:** Verifique se a `ConnectionStrings:DefaultConnection` está configurada no `appsettings.json`

### Erro de Chave JWT

```
Jwt:Key deve ter pelo menos 16 caracteres
```
**Solução:** Configure uma chave JWT com no mínimo 16 caracteres no `appsettings.json`

### Erro 401 Unauthorized

**Causas comuns:**
- Token não fornecido
- Token expirado (2 horas)
- Token inválido
- Formato incorreto do header (deve ser: `Bearer {token}`)

### Erro de CORS

```
Access to fetch has been blocked by CORS policy
```
**Solução:** Adicione a origem do seu frontend na configuração CORS em `Program.cs`

## Desenvolvimento

### Adicionando Novas Migrations

```bash
dotnet ef migrations add NomeDaMigration
dotnet ef database update
```

### Executando Testes

```bash
dotnet test
```

## Contribuindo

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova feature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

## Licença

Este projeto é privado e proprietário.

## Suporte

Para questões e suporte, entre em contato:
- Email: contato@perfilrenova.com

---

**Versão:** 1.0.0
**Última Atualização:** Janeiro 2025
