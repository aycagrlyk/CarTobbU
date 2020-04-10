﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Google;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.Networking;
public class GoogleSignInDemo : MonoBehaviour
{

    public Text infotext;
    public string webClientId = "<client id>";
    
    public static string userEmail;
    private FirebaseAuth auth;
    private GoogleSignInConfiguration configuration;

    private void Awake(){
        configuration = new GoogleSignInConfiguration{ WebClientId = webClientId, RequestEmail = true, RequestIdToken = true};
        CheckFirebaseDependencies();
    }

    private void CheckFirebaseDependencies(){
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith( task =>
        {
            if(task.IsCompleted){
                if(task.Result == DependencyStatus.Available)
                    auth = FirebaseAuth.DefaultInstance;
                else
                    AddToInformation("Could not resolve all Firebase dependencies: " + task.Result.ToString());
            }
            else{
                AddToInformation("Dependency check was not completed. Error: "+ task.Exception.Message);
            }

        });
    }

    public void SignInWithGoogle(){
        OnSignIn();
    }

    private void OnSignIn(){
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;
        GoogleSignIn.Configuration.RequestEmail = true;
        AddToInformation("calling sign in");

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
    }

    public void SignOutFromGoogle(){
        OnSignOut();
    }

    private void OnSignOut(){
        AddToInformation("calling sign out");
        GoogleSignIn.DefaultInstance.SignOut();
    }

    private void AddToInformation(string str){
        infotext.text += "\n" + str;
    }

    public void OnDisconnect(){
        AddToInformation("calling disconnect");
        GoogleSignIn.DefaultInstance.Disconnect();
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task){
        if(task.IsFaulted){
            using (IEnumerator<Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator()){
                if(enumerator.MoveNext()){
                    GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
                    AddToInformation("Got error: "+error.Status+" "+error.Message);
                }
                else{
                    AddToInformation("Got unexpected Exception?? "+task.Exception);
                }
            }

        }
        else if (task.IsCanceled){
            AddToInformation("canceled");
        }
        else{
            AddToInformation("Welcome: "+task.Result.DisplayName + "!");
            AddToInformation("Email= "+ task.Result.Email);
            Debug.Log("email: "+ task.Result.Email);
            AddToInformation("Google ID Token = " + task.Result.IdToken);
            AddToInformation("Email= "+task.Result.Email);
            SignInWithGoogleOnFirebase(task.Result.IdToken); 
        }


    }

    private void SignInWithGoogleOnFirebase(string idToken){
        Credential credential = GoogleAuthProvider.GetCredential(idToken,null);
        
        auth.SignInWithCredentialAsync(credential).ContinueWith( task =>
        {
            AggregateException ex = task.Exception;
            if(ex != null){
                if(ex.InnerExceptions[0] is FirebaseException inner && (inner.ErrorCode != 0))
                    AddToInformation("\nError code= " + inner.ErrorCode + "Message = " + inner.Message);
                
            }
            else{
                    AddToInformation("sign in successful");
                    Debug.Log(task.Result.Email);
                    userEmail = task.Result.Email;
                    CallLoginWithEmail(userEmail);
                    UnityEngine.SceneManagement.SceneManager.LoadScene(3);
            }
            
        
        
        });
    }

    public void OnSignInSilently(){
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;
        AddToInformation("calling signin silently");
    
        GoogleSignIn.DefaultInstance.SignInSilently().ContinueWith(OnAuthenticationFinished);
    }

    public void OnGamesSignIn(){
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = true;
        GoogleSignIn.Configuration.RequestIdToken = false;

        AddToInformation("calling games signin");
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
    }


    public void CallLoginWithEmail(string eu){
        StartCoroutine(LoginWithEmail(eu));
    }

    IEnumerator LoginWithEmail(string emailuser){
        // email = userEmail;
        var uwr = new UnityWebRequest("https://smart-car-park-api.appspot.com/user/googleSignIn/" + emailuser, "POST");
    
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError){
            Debug.Log("giderken hata oldu "+uwr.error);
        }
        else{
            userEmail = uwr.downloadHandler.text;
            UnityEngine.SceneManagement.SceneManager.LoadScene(3);
        }
    }

    


}
