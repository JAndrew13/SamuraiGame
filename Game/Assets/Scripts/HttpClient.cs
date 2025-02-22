using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;


// this Async workflow workaround uses Unity Web Requests, but avoids the forced co-routine.
namespace Assets.Scripts
{
    public static class HttpClient 
    {
        public static async Task<T> Get<T>(string endpoint) {
            var getRequest = CreateRequest(endpoint);
            await getRequest.SendWebRequest();

            while (!getRequest.isDone) await Task.Delay(10);
            return JsonConvert.DeserializeObject<T>(getRequest.downloadHandler.text);
        }

        public static async Task<T> Post<T>(string endpoint, object payload) { 
        
            var postRequest = CreateRequest(endpoint, RequestType.POST, payload);
            await postRequest.SendWebRequest();

            while (!postRequest.isDone) await Task.Delay(10);
            return JsonConvert.DeserializeObject<T>(postRequest.downloadHandler.text);
        }       
            
        private static UnityWebRequest CreateRequest(string path, RequestType type = RequestType.GET, object data = null) {
            var request = new UnityWebRequest(path, type.ToString());

            if (data != null)
            {
                var bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }

        private static void AttachHandler(UnityWebRequest request, string key, string value) {
            request.SetRequestHeader(key, value);
        }
    }

    public enum RequestType { 
        GET = 0,
        POST = 1,
        PUT = 2,
        DELETE = 3,
    }
}
