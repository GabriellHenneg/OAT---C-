using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Por favor, insira o caminho do arquivo que deseja resumir:");
        string filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("O caminho do arquivo não pode ser vazio.");
            return;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Arquivo não encontrado. Verifique o caminho e tente novamente.");
            return;
        }

        try
        {
            string fileContent = await File.ReadAllTextAsync(filePath);
            string resumo = await RetornoIA(fileContent);
            Console.WriteLine("Resumo gerado:");
            Console.WriteLine(resumo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu um erro: {ex.Message}");
        }
    }

    public static async Task<string> RetornoIA(string content)
    {
        var requestBody = new
        {
            model = "llama3",
            messages = new[]
            {
                new { role = "user", content = content }
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = $"Erro na requisição: {response.StatusCode}";
                Console.WriteLine(errorMsg);
                return "Desculpe, não consegui responder agora.";
            }

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(contentStream);

            var result = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return result?.Trim() ?? "Resposta vazia.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao comunicar com a IA: {ex.Message}");
            return "Desculpe, ocorreu um erro ao processar sua solicitação.";
        }
    }
}
