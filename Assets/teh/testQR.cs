using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.UI;

public class testQR : MonoBehaviour
{public Text displayText;
    string code = "https://qr1.me-qr.com/text/tjnnfm7v";


    // Dictionary: Code → Data
    Dictionary<string, string> dataDictionary = new Dictionary<string, string>()
    {
        { "1235", "Scissors QR code used for cutting ropes" },
        { "7889", "Dryer machines used for warming" },
        { "5678", "flask used for containing multiple chemicals" }
    };

    
    public void Start123(string u)
    {
        code = u;
        StartCoroutine(GetData());
    }

    IEnumerator GetData()
    {
        //UnityWebRequest request = UnityWebRequest.Get(url);
        //yield return request.SendWebRequest();

       // if (request.result == UnityWebRequest.Result.Success)
        //{
          //  string html = request.downloadHandler.text;

          //  Match match = Regex.Match(html, "<p>(.*?)</p>", RegexOptions.Singleline);

          //  if (match.Success)
          //  {
          //      string code = match.Groups[1].Value.Trim();

                //Debug.Log("QR Code: " + code);

                // 🔹 Lookup in dictionary
                if (dataDictionary.ContainsKey(code))
                {
                    string result = dataDictionary[code];

                    Debug.Log("Matched Data: " + result);

                    if (displayText != null)
                    {
                        
                        displayText.text = result;
                    }
                }
                else
                {
                    Debug.Log("Code not found in dictionary");

                    if (displayText != null)
                        displayText.text = "Unknown Code";
                }
          //  }
       // }
       // else
       // {
       //     Debug.Log("Error: " + request.error);
       // }
       yield return null;
    }
}