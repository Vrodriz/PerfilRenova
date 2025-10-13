
namespace PerfilRenovaWeb.api.Dtos;


public class ClientResponseDtos
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Cnpjcpf { get; set; } = string.Empty;
    public string DataValidade { get; set; } = string.Empty;
    public string Bloqueado { get; set; } = "N";
    public bool Pendente { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

/// <summary>
/// DTO para requisição de atualização compativel 
/// </summary>

public class UpdatesClientesRequestDTO
{
    public string? Bloqueado { get; set; }
    public string? DataValidade { get; set; }
    public bool? Pendente { get; set; }
}

///<summary>
/// DTO para resposta da lista de paginação
/// </summary>

public class GetCLientesResponseDTO
{

    public List<GetCLientesResponseDTO> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int Pagesize { get; set; }
    public int TotalPages { get; set; }

}

///<summary>
/// DTO para parâmetros de filtro e buscas
///</summary>

public class ClientFilterDto
{
    public string? Search { get; set; } // Busca por CNPJ ou Razão Social
    public string? Status { get; set; } // "Ativos", "Bloqueados", "Vencidos", "Pendentes"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

///<summary>
/// DTO para resposta paginada de clientes
///</summary>

public class PaginatedClientsResponseDto
{
    public List<ClientResponseDtos> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

///<summary>
/// DTO para requisição de atualização em lote
///</summary>

public class BulkUpdateRequestDto
{
    public List<int> ClientIds { get; set; } = new();
    public string? Bloqueado { get; set; }
    public string? DataValidade { get; set; }
    public bool? Pendente { get; set; }
}

///<summary>
/// DTO para resposta de atualização em lote
///</summary>

public class BulkUpdateResponseDto
{
    public List<ClientResponseDtos> UpdatedClients { get; set; } = new();
    public int UpdatedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

///<summary>
/// DTO genérico de mensagem de erro
/// </summary>

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}
