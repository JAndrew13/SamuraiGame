using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.Networking;
using Assets.Scripts;

public class LoginManager : MonoBehaviour
{
    public InputField usernameInput;
    public InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public Text feedbackText;

    private HttpClient httpClient;

    void Start()
    {
        httpClient = GetComponent<HttpClient>();
        loginButton.onClick.AddListener(OnLogin);
        registerButton.onClick.AddListener(OnRegister);
    }

    void OnLogin()
    {
        var user = new { Username = usernameInput.text, Password = passwordInput.text };
        StartCoroutine(httpClient.Post("authentication/login", user, OnLoginResponse));
    }

    void OnRegister()
    {
        var user = new { Username = usernameInput.text, Password = passwordInput.text };
        StartCoroutine(httpClient.Post("authentication/register", user, OnRegisterResponse));
    }

    void OnLoginResponse(UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
            var token = response["Token"];
            PlayerPrefs.SetString("JWTToken", token);
            feedbackText.text = "Login successful!";
            // Load the next scene or