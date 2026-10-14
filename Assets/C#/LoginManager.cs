using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class LoginManager : MonoBehaviour
{
    void Start()
    {
        
    }

    public void SignIn(){
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success){
            Debug.Log("Google Play Games Sign in successful");
        }
        else{
            Debug.Log("Google Play Games Sign in failed");
        }
    }


}
