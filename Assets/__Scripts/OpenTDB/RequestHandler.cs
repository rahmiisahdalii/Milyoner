using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace OpenTDB
{
    public static class RequestHandler
    {
        public static bool EnableDebug = false;

public static Task<List<Question>> GetQuestions(QuestionRequest request)
{
    try
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("questions");
        if (jsonFile != null)
        {
            if (EnableDebug)
                Debug.Log("Local JSON file loaded successfully.");
            
            List<Question> questions = JsonConvert.DeserializeObject<List<Question>>(jsonFile.text);

            // Zorluk seviyesine göre filtreleme
            var filteredQuestions = questions
                .Where(q => q.difficulty == request.difficulty)
                .ToList();

            return Task.FromResult(filteredQuestions);
        }
        else
        {
            Debug.LogError("questions.json not found in Resources folder.");
            return Task.FromException<List<Question>>(new FileNotFoundException("questions.json not found."));
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogError("Error while loading local JSON: " + ex.Message);
        return Task.FromException<List<Question>>(ex);
    }
}


    }
}
