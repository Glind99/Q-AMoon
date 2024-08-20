using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // Add this for JSON handling

class Program
{
    private static readonly string endpoint = "https://gustavqalabb.cognitiveservices.azure.com/language/:query-knowledgebases?projectName=GustavQA&api-version=2021-10-01&deploymentName=production";
    private static readonly string apiKey = "067b86fd816742c2af2e8800bcaae960";
    private static readonly string translatorApiKey = "bcaedfedc7034590b74d37cdc370a2a2";
    private static readonly string translatorEndpoint = "https://api.cognitive.microsofttranslator.com/";
    private static readonly string translatorLocation = "northeurope";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Hey and welcome to our Q&A about the moonlanding!");
        Console.WriteLine("Ask a question about the program!");
        Console.Write("Question: ");

        while (true)
        {
            string question = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(question))
                break;
            try
            {
                string answer = await GetAnswerFromQnAService(question);

                Console.WriteLine("Would you like to translate the answer to Swedish? (yes/no): ");
                string translateChoice = Console.ReadLine().Trim().ToLower();

                if (translateChoice == "yes")
                {
                    string translatedAnswer = await TranslateText(answer, "en", "sv");
                    Console.WriteLine($"Answer (translated): {translatedAnswer}");
                }
                else
                {
                    Console.WriteLine($"Answer: {answer}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine("Press enter to quit Q&A!");
            Console.Write("Next Question: ");
        }
    }

    static async Task<string> GetAnswerFromQnAService(string question)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            var requestBody = new
            {
                question = question
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(endpoint, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    var result = JsonSerializer.Deserialize<QnAResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result?.Answers != null && result.Answers.Length > 0)
                    {
                        return result.Answers[0].Answer.Trim();
                    }
                    else
                    {
                        return "Sorry, I do not have the answer for that.";
                    }
                }
                catch (JsonException jsonEx)
                {
                    return $"JSON deserialization error: {jsonEx.Message}";
                }
            }
            else
            {
                return $"HTTP error: {response.StatusCode}";
            }
        }
    }

    static async Task<string> TranslateText(string text, string fromLanguage, string toLanguage)
    {
        string route = $"/translate?api-version=3.0&from={fromLanguage}&to={toLanguage}";
        string requestBody = "[{'Text':'" + text + "'}]";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", translatorApiKey);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", translatorLocation);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.PostAsync(translatorEndpoint + route, new StringContent(requestBody, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                JArray jsonResponse = JArray.Parse(result);
                return jsonResponse[0]["translations"][0]["text"].ToString();
            }
            else
            {
                throw new Exception($"Translation failed with status code: {response.StatusCode}");
            }
        }
    }
}

public class QnAResponse
{
    public QnAAnswer[] Answers { get; set; }
}

public class QnAAnswer
{
    public string Answer { get; set; }
    public float ConfidenceScore { get; set; }
}

