using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LisansUstuBasvuruSistemi.YokApi
{
    public class ServiceManager
    {

        //var client = new ServiceManager();
        //var requestModel = new StudentAddress
        //{
        //    Id = _address.Id,
        //    Address = txtAddress.Text,
        //    StudentId = StudentContraint.LoggedInStudent.Id
        //};
        //var result = await client.Put<StudentAddress, StudentAddress>(requestModel,
        //    "api/StudentAddresses/" + requestModel.Id);


        //string authInfo = "raj" + ":" + "34sddff";
        //authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);

        private readonly string ServiceBaseUrl = "https://servisler.yok.gov.tr";
        //pedagojikFormasyonListele
        //-<resource path = "/rest/obs/pedagojikformasyonalanlari" >
        private Task<HttpClient> GetClientAsync()
        {
            var client = new HttpClient();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            client.DefaultRequestHeaders.Add("Accept", "application/json");

            string authInfo = "126982" + ":" + "kzB)s2U796";
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);
            return Task.FromResult(client);
        }
        private HttpClient GetClient()
        {
            var client = new HttpClient();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            client.DefaultRequestHeaders.Add("Accept", "application/json");

            string authInfo = "126982" + ":" + "kzB)s2U796";
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);
            return client;
        }
        //public async Task<Student> Login(LoginRequestModel model)
        //{
        //    var client = await GetClient();

        //    var jsonRequestData = JsonConvert.SerializeObject(model);

        //    var result = await client.PostAsync(
        //        ServiceBaseUrl + "api/Login",
        //        new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));

        //    if (result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        var returnData = await result.Content.ReadAsStringAsync();
        //        var returnModel = JsonConvert.DeserializeObject<Student>(returnData);
        //        return returnModel;
        //    }
        //    else
        //    {
        //        return new Student();
        //    }
        //}

        public async Task<System.Net.HttpStatusCode> Postx<T, K>(K model, string urlParam)
        {
            var client = await GetClientAsync();

            var jsonRequestData = JsonConvert.SerializeObject(model);

            var result = await client.PostAsync(
                ServiceBaseUrl + urlParam,
                new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));

            return result.StatusCode;
            //if (result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    var returnData = await result.Content.ReadAsStringAsync();
            //    var returnModel = JsonConvert.DeserializeObject<T>(returnData);
            //    return returnModel;
            //}
            //else
            //{
            //    return default(T);
            //}
        }
        public async Task<HttpResponseMessage> PostAsync<T, K>(K model, string urlParam)
        {
            var client = await GetClientAsync();

            var jsonRequestData = JsonConvert.SerializeObject(model);

            var result = await client.PostAsync(
                ServiceBaseUrl + urlParam,
                new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));

            return result;
            //if (result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    var returnData = await result.Content.ReadAsStringAsync();
            //    var returnModel = JsonConvert.DeserializeObject<T>(returnData);
            //    return returnModel;
            //}
            //else
            //{
            //    return default(T);
            //}
        }
       
        public static string HttpPostJson(string url, string method, string json, int timeoutDuration = 0)
        {
            var methodUrl = (url + "/" + method).Replace(" ", string.Empty);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(methodUrl);
            httpWebRequest.Timeout = timeoutDuration == 0 ? 180000 : timeoutDuration;

            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                return result;
            }
        }

     
        //public async Task<List<Lesson>> GetLessons()
        //{
        //    var client = await GetClient();
        //    var result = await client.GetStringAsync(ServiceBaseUrl + "api/Lessons");
        //    var model = JsonConvert.DeserializeObject<List<Lesson>>(result);
        //    return model;
        //}

        public async Task<T> Get<T>(string urlParam)
        {
            var client = await GetClientAsync();
            var result = await client.GetStringAsync(ServiceBaseUrl + urlParam);
            var model = JsonConvert.DeserializeObject<T>(result);
            return model;
        }
        //public async Task<string> Get(string urlParam)
        //{
        //    var client = await GetClient();
        //    var result = await client.GetStringAsync(ServiceBaseUrl + urlParam);

        //    return result;
        //}

        public async Task<T> Put<T, K>(K model, string urlParam)
        {
            var client = await GetClientAsync();

            var jsonRequestData = JsonConvert.SerializeObject(model);

            var result = await client.PutAsync(
                ServiceBaseUrl + urlParam,
                new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));

            if (result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var returnData = await result.Content.ReadAsStringAsync();
                var returnModel = JsonConvert.DeserializeObject<T>(returnData);
                return returnModel;
            }
            else
            {
                return default(T);
            }
        }

        public async Task<bool> Delete<T, K>(K model, string urlParam)
        {
            var client = await GetClientAsync();
            var result = await client.DeleteAsync(ServiceBaseUrl + urlParam);

            return result.IsSuccessStatusCode;
            //if (result.IsSuccessStatusCode)
            //{
            //    var respose = await result.Content.ReadAsStringAsync();
            //    return JsonConvert.DeserializeObject<T>(respose);
            //}
            //else
            //{
            //    return default(T);
            //}
        }
    }
}