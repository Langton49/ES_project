using System.Collections;
using System.Linq;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System;
using System.Threading.Tasks;
using MoonshineStudios.CharacterInputController;

[DefaultExecutionOrder(-4)]
public class riddleManager : MonoBehaviour
{

    public List<GameObject> cityLocations = new List<GameObject>();
    private GameObject[] allObjects;
    [HideInInspector]
    public string passPhrase;
    private OpenAIClient api;
    private int numberOfArtifacts = 2;

    #region Struct Initialization
    public struct HidingPlaceDetails
    {
        public GameObject artifact {  get; set; }
        public string location { get; set; }
        public string riddle {  get; set; }

        public HidingPlaceDetails(GameObject artifact, string location, string riddle)
        {
            this.artifact = artifact;
            this.location = location;
            this.riddle = riddle;
        }
    }

    #endregion

    [HideInInspector]
    public HidingPlaceDetails[] hidingPlaces { get; private set; }

    public event Action onHidingPlacesFilled;
    public event Action indexUpdated;
    public event Action<string> allArtifactsFound;
    public event Action<string> getPassphrase;

    public gameController gameController;

    public int currentIndex { get; private set; } = 0;

    private async void Awake()
    {
        Authenticate();
        allObjects = GameObject.FindGameObjectsWithTag("artifact");
        gameController.numArtifactsChanged += SetNumberOfArtifacts;
    }

    #region Methods
    private void Authenticate()
    {
        api = new OpenAIClient(Utils.apiKey);
    }

    public async void SetNumberOfArtifacts(int count)
    {
        numberOfArtifacts = count;
        hidingPlaces = getHidingPlaces();
        await generateRiddles();
        await setPassphrase();
    }

    #region Get All Hiding Places
    private HidingPlaceDetails[] getHidingPlaces()
    {
        GameObject[] result = allObjects.ToArray();
        HidingPlaceDetails[] details = new HidingPlaceDetails[numberOfArtifacts];

        System.Random random = new System.Random();

        for (int i = allObjects.Length - 1; i >= 0; i--)
        {
            int randomIndex = random.Next(0, i + 1);
            GameObject temp = result[i];
            result[i] = result[randomIndex];
            result[randomIndex] = temp;
        }

        GameObject[] hidingPlaces = result.Take(numberOfArtifacts).ToArray();

        for ( int i = 0; i < hidingPlaces.Length; i++)
        {
            if (hidingPlaces[i].GetComponent<BoxCollider>() == null)
            {
                hidingPlaces[i].AddComponent<BoxCollider>();
            }
            hidingPlaces[i].GetComponent<BoxCollider>().isTrigger = true;
            hidingPlaces[i].GetComponent<BoxCollider>().size = new Vector3(5f, 5f, 5f);
            hidingPlaces[i].AddComponent<grabUi>();
            HidingPlaceDetails detail = new HidingPlaceDetails(hidingPlaces[i], findLocation(hidingPlaces[i]), "-");
            details[i] = detail;
        }
       
        return details;
    }
    #endregion

    private string findLocation(GameObject hidingPlace)
    {
        foreach (GameObject location in cityLocations)
        {
            BoxCollider collider = location.GetComponent<BoxCollider>();

            if (collider.bounds.Contains(hidingPlace.transform.position))
            {
                return location.transform.parent.name;
            }
        }

        return null;
    }

    public async Task generateRiddles()
    {

        #region API Request
        var chatRequest = new ChatRequest(
            messages: new List<Message>
            {
            new Message(
                role: Role.System,
                content: @"You are a riddle master creating engaging clues for a treasure hunt game. 
            When given a list of objects and their locations in different districts, create playful riddles that:
            1. Reference both the object and its district location
            2. Keep riddles short (2-4 lines each)
            3. Make them challenging but solvable
            4. Maintain a consistent, fun tone
            5. Avoid explicitly stating the object name
            6. Include subtle hints about the district based on the context of each district:
                - 'Market_Place': A district known for its bustling commerce.
                - 'Bean_District': A district known for its numerous cafe's and restaurants.
                - 'City_Square': A place known for gatherings, interactions, art and street performances.
                - 'Solstice_Bridge': A bridge that connects the two towns of the city.
                - 'Regal_Heights': A place of luxury where the nobles stay known for luxurious homes. 
                - 'Craftsman_Alley': A set of buildings where blacksmiths, sculptors and seamstresses work and craft. An alley near City_Square
                - 'Fish_Market': A marketplace near the sea where fishermen sell their fish.
                - 'Horace_Alley': An alley just behind Market_Place, known for habouring the merchants who sell their good at Market_Place
                - 'OceanFront': A part of the city that faces the entire ocean known for its monument, giant stone steps that lead to the oceans depth.
                - 'River_Avenue': A stretch of road that runs alongside the river that divides the city in two.
                - 'Mermaid_River_Bank': A small piece of sandy land that lies on the bank of the river that divides the city in two.
                - 'Avenue_River_Bank': A small piece of sandy land that lies on the bank of the river that divides the city in two. Named for be near River_Avenue.
                
            7. Ensure each riddle has no line breaks between each line. Represent line breaks as '\n'
            8. Write each entire riddle in a single line, with each riddle on seperate lines.
            9. Do not include any other text in the response, only the riddles.

            Example:
            For 'Fountain (MarketDistrict)'
            'In the bustling heart where merchants meet,\nListen for my endless watery beat.\nCoins make wishes as they sink below,\nIn this square where trade and dreams both flow.'
            For 'Statue (RegalHeights)'
            'In the serene waterside where the river's flow,\nI am a symbol of life's gentle breeze.\nMy form is a testament to the river's grace,\nIn this place where the river's flow is unceasing.'
            "
            ),
            new Message(
                role: Role.User,
                content: $"Generate riddles based on the following objects and their districts: {string.Join(", ", hidingPlaces.Select(x => $"{x.artifact.name} ({x.location})"))}"
            )
            },
            model: Model.GPT4o,
            temperature: 0.9
        );
        #endregion

        var result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        string[] riddles = result.FirstChoice.Message.Content.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string riddlesUnedited = result.FirstChoice.Message.Content.ToString();
        Debug.Log(riddles.Length);
        Debug.Log(riddlesUnedited);
        for(int i = 0; i < hidingPlaces.Length; i++)
        {
            hidingPlaces[i].riddle = riddles[i].Replace("\\n", "\n");
        }
        onHidingPlacesFilled?.Invoke();
        printHidingPlaces();

    }

    public async Task setPassphrase()
    {
        #region API Call
        var chatReq = new ChatRequest(
            messages: new List<Message>
            {
            new Message( 
                role: Role.System,
                content: @"Generate a random word. 
                The word must be a real word, not made up. Respond with only the word, 
                nothing else."
            ),

            new Message(
                role: Role.User,
                content: @"Generate random word."
                )
            },
            model: Model.GPT4o,
            temperature: 0.9
            );

        var result = await api.ChatEndpoint.GetCompletionAsync(chatReq);
        #endregion
        string passphrase = result.FirstChoice.Message.Content.ToString();
        passPhrase = passphrase;
        getPassphrase?.Invoke(passPhrase);
        Debug.Log(passPhrase);
        return;

    }


    public void updateIndex()
    {
        currentIndex++;
        if (currentIndex == hidingPlaces.Length)
        {
            allArtifactsFound?.Invoke(passPhrase);
        }
        else
        {
            indexUpdated?.Invoke();
        }
        Debug.Log("Current index: " + currentIndex);
    }

    private void OnDestroy()
    {
        gameController.numArtifactsChanged -= SetNumberOfArtifacts;
    }


    #endregion

    #region Debugging
    private void printHidingPlaces()
    {
        foreach (HidingPlaceDetails hidingPlace in hidingPlaces)
        {
            Debug.Log(hidingPlace.artifact.name + "--" + hidingPlace.location + "--" + hidingPlace.riddle);
        }
    }
    #endregion
}
