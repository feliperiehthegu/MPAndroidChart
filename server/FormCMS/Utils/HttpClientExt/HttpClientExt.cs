using System.Text;
using FluentResults;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FormCMS.Utils.HttpClientExt;

public static class HttpClientExt
{
    
    private static readonly JsonSerializerOptions JsonSerializerOptions= new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async Task<Result<string>> GetStringResult(this HttpClient client, string uri)
    {
        try
        {
            var res = await client.GetAsync(uri);
            return await res.ParseString();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public static async Task<Result<T>> GetResult<T>(this HttpClient client, string uri)
    {
        var res = await client.GetAsync(uri);
        return await res.ParseResult<T>(JsonSerializerOptions);
    }
    public static async Task<Result> GetResult(this HttpClient client, string uri)
    {
        var res = await client.GetAsync(uri);
        return await res.ParseResult();
    }
    
    public static async Task<Result<string>> PostFileResult(this HttpClient client, string url, string field, IEnumerable<(string,byte[])> files)
    {
        using var content = new MultipartFormDataContent();
        foreach (var (fileName,bytes) in files)
        {
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(fileContent, field, fileName); 
        }
        var response = await client.PostAsync(url, content);
        return await response.ParseString();
    }
    
    public static async Task<Result<T>> PostResult<T>(this HttpClient client, string url, object payload )
    {
        var res = await client.PostAsync(url, Content(payload,JsonSerializerOptions));
        return await res.ParseResult<T>(JsonSerializerOptions);
    }

    public static async Task<Result> PostResult( this HttpClient client, string url, object payload)
    {
        var res= await client.PostAsync(url, Content(payload,JsonSerializerOptions));
        return await res.ParseResult();
    }

    public static async Task<Result<T>> PutResult<T>(this HttpClient client, string url, object payload)
    {
        var res = await client.PutAsync(url, Content(payload, JsonSerializerOptions));
        return await res.ParseResult<T>(JsonSerializerOptions);
    }
    public static async Task<Result> PutResult(this HttpClient client, string url, object payload)
    {
        var res = await client.PutAsync(url, Content(payload, JsonSerializerOptions));
        return await res.ParseResult();
    }


    public static async Task<Result> PostAndSaveCookie(this HttpClient client, string url, object payload)
    {
        var response = await client.PostAsync(url, Content(payload,JsonSerializerOptions));
        client.DefaultRequestHeaders.Add("Cookie", response.Headers.GetValues("Set-Cookie"));
        return await response.ParseResult();
    }
    
    public static async Task<Result> DeleteResult(this HttpClient client, string uri)
    {
        var res = await client.DeleteAsync(uri);
        return await res.ParseResult();
    }
    
    private static async Task<Result> ParseResult(this HttpResponseMessage msg)
    {
        var str = await msg.Content.ReadAsStringAsync();
        if (!msg.IsSuccessStatusCode)
        {
            return Result.Fail(
                $"Fail to {msg.RequestMessage?.Method} {msg.RequestMessage?.RequestUri}, statusCode={msg.StatusCode} message= {str}");
        }
        return Result.Ok();
    }

    private static async Task<Result<string>> ParseString(this HttpResponseMessage msg)
    {
        var str = await msg.Content.ReadAsStringAsync();
        if (!msg.IsSuccessStatusCode)
        {
            return Result.Fail(
                $"Fail to {msg.RequestMessage?.Method} {msg.RequestMessage?.RequestUri},statusCode={msg.StatusCode}, message= {str}");
        }
        return Result.Ok(str);
    }

    private static async Task<Result<T>> ParseResult<T>(this HttpResponseMessage msg, JsonSerializerOptions? options)
    {
        var str = await msg.Content.ReadAsStringAsync();
        if (!msg.IsSuccessStatusCode)
        {
            return Result.Fail(
                $"fail to {msg.RequestMessage?.Method} {msg.RequestMessage?.RequestUri}, statusCode={msg.StatusCode}, message= {str}");
        }

        var item = JsonSerializer.Deserialize<T>(str,options);
        return item is null ? Result.Fail("Fail to Deserialize") : item;
    }

    private static StringContent Content(object payload, JsonSerializerOptions?options) =>
        new(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");

}