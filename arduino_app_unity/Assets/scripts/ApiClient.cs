#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;

    public ApiClient(string accessToken)
    {
        _accessToken = accessToken;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://lichess.org/"); // Zmieñ na adres API, z którym chcesz siê po³¹czyæ
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    public async Task<string> ApiGet(string endpoint)
    {
        string responseContent = string.Empty;

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                responseContent = await response.Content.ReadAsStringAsync();
            }
            else
            {
                Debug.LogError("Request failed with status code: " + response.StatusCode);
                Debug.LogError(endpoint);
                Debug.LogError(response);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
        }

        return responseContent;
    }

    public async Task<string> ApiPost(string endpoint, Dictionary<string, string>? formData = null)
    {
        string responseContent = string.Empty;

        try
        {
            HttpContent? content = null;

            if (formData != null)
            {
                content = new FormUrlEncodedContent(formData);
            }

            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                responseContent = await response.Content.ReadAsStringAsync();
            }
            else
            {
                Debug.LogError("Request failed with status code: " + response.StatusCode);
                Debug.LogError(endpoint + content);
                Debug.LogError(response);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error occurred: " + ex.Message);
            Debug.LogError(endpoint + formData);
        }

        return responseContent;
    }
}