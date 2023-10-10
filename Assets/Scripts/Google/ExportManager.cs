using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using CsvHelper;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;


// I am making it a singleton because otherwise each time we load a scene and we reach start, the csv file will be reset.
// I need the CSV file to be created and reset only ONCE at the very start of the game
public class ExportManager : SimpleSingleton<ExportManager>, IRegistrableService
{    
    [Header("Google")]
    [SerializeField]
    DataMigrationSettings settings;
    string rangeEnd = "!A2";

    [Header("CSV")]
    string CSVDirectory = Application.dataPath;
    string CSVPath;
    string CSVName = "CVSExport.csv";
    char CSVseparator = ',';

    GameManager gameManager;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
        ServiceLocator.Instance.Register<ExportManager>(this);
        CreateCSVWithTitles();
    }
    void Start()
    {
        gameManager = ServiceLocator.Instance.Get<GameManager>();
    }

    //this function will collect the data we need to export and do a syntax check to make sure the data we are exporting matches the title columns on the spreadsheets
    List<object> CollectAndCheckData(TrackData trackData, string interactionId, Emotions response)
    {
        List<object> data = new List<object>();
        DateTime exportTime = GetCETTime();
        foreach (var item in settings.newSheetProperties.columnTitles)
        {
            switch (item)
            {
                case ("CET Time stamp"):
                    data.Add(exportTime.ToString());
                    break;
                case ("Export Event ID"):
                    data.Add(GetExportEventID(exportTime.ToString()));
                    break;
                case ("Track ID"):
                    data.Add(trackData.trackID);
                    break;
                case ("Annotation"):
                    data.Add(response.ToString());
                    break;
                case ("Interaction ID"):
                    data.Add(interactionId);
                    break;
                case ("User Name"):
                    data.Add(gameManager.GetPlayerName());
                    break;
                case ("User ID"):
                    data.Add(gameManager.GetPlayerID());
                    break;
                case ("Game Session Index"):
                    data.Add(gameManager.GetGameSessionIndex().ToString());
                    break;
                case ("Application Version"):
                    data.Add(Application.version);
                    break;
                case ("Configuration ID"):
                    data.Add(gameManager.GetConfigurationFileID());
                    break;
                case ("Time of Day"):
                    data.Add(GetTimeOfDay(exportTime));
                    break;
                default: // if it doesn't match any case
                    Debug.LogError("a column title in the export sheet does not match any data. Please ensure that the list of column titles under Data Migration Settings -> New Spreadsheet properties are correct.");
                    break;
            }
        }
        return data;
    }

    public void ExportData(TrackData trackData, string interactionId, Emotions response)
    {
        List<object> dataToExport = CollectAndCheckData( trackData, interactionId, response);
        if (dataToExport.Count != settings.newSheetProperties.columnTitles.Length) // the data we have collected does not match what we require for the spreadsheet
            return;
        ExportToGoogleSheets(dataToExport);
        ExportToCSV(dataToExport);
    }



    #region GOOGLE EXPORT
    void ExportToGoogleSheets(IList<object> dataToExport)
    {
        var values = new List<IList<object>> { dataToExport };
        bool success = GoogleSheets.PushData(settings.spreadsheetID, settings.exportSheetID, values, settings.exportSheetName + rangeEnd);
        if (success) 
            Debug.Log("Recorded the following data to Google Sheets: " + string.Join(" ,", dataToExport));
    }

    void AddTitlesToGoogleSheets()
    {

    }

    #endregion
    #region CSV EXPORT
    void ExportToCSV(List<object> dataToExport)
    {
        VerifyFile();
        using (var writer = new StreamWriter(CSVPath, true))
        {
            writer.WriteLine(string.Join(CSVseparator, dataToExport));
        }
        Debug.Log("Recorded the following data to CSV: " + string.Join(" ,", dataToExport));
    }
    void VerifyFile() // check that the CSV file exists, if it doesn't then create one
    {
        if(!File.Exists(CSVPath))
        {
            CreateCSVWithTitles();
        }
    }
    void CreateCSVWithTitles()
    {
        try
        {
            // add the titles first, remove everything that's already there is something is there 
            CSVName = ChooseNameForCSV();
            CSVPath = Path.Combine(CSVDirectory, CSVName);
            using (var writer = new StreamWriter(CSVPath, false))
            {
                writer.WriteLine(string.Join(CSVseparator, settings.newSheetProperties.columnTitles));
            }
            Debug.Log("Created CSV in path: " + CSVPath);
        }
        catch (Exception e)
        {
            Debug.LogError("Could not write and save CSV. Error is: " + e.Message);
        }
    }

    string  ChooseNameForCSV()
    {
        return  "CVSExport"+".csv";
    }

    // returns true for success and false for failure 
    public bool SendCSVByEmail()
    {
        // add configuration name to the email 

        VerifyFile();
        try 
        {
            if(string.IsNullOrEmpty(settings.researchersEmail))
            {
                Debug.LogError("Email to send to is unknown. Please set it up in the Data Migration Settings window.");
            }
            string emailBody = "Hello,\n the data you have requested is attached to this email.";
            EmailSender.SendEmail("Collected Data", emailBody, settings.researchersEmail,CSVPath);
            Debug.Log("Successfully send collected data CSV to email");
            return true;
        }
        catch (Exception e) 
        {
            Debug.LogError("Could not send CSV by email. Error is: " + e.Message);
            return false;

        }
    }
    #endregion
    #region VALUES
    string GetExportEventID(string exportTime)
    {
        // in UUID format 
        // YYYY:MM:DD:HH:MM:SS_PlayerID_GameSessionIndex_cc867280-68a7-4737-8676-0f14d2ae1b1f for data point id
        Guid randomGuid = Guid.NewGuid();
        string randomGuidString = randomGuid.ToString();
        return exportTime+"_"+gameManager.GetPlayerID()+"_"+gameManager.GetGameSessionIndex()+"_"+ randomGuidString;
    }

    DateTime GetCETTime()
    {
        // Define the CET time zone
        TimeZoneInfo cetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        // Get the current UTC time
        DateTime utcNow = DateTime.UtcNow;
        // Convert UTC time to CET time
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, cetTimeZone);
    }

    string GetTimeOfDay(DateTime time)
    {
        int hour = time.Hour;

        if (hour >= 5 && hour < 12)
        {
            return "Morning";
        }
        else if (hour >= 12 && hour < 17)
        {
            return "Afternoon";
        }
        else if (hour >= 17 && hour < 21)
        {
            return "Evening";
        }
        else
        {
            return "Night";
        }
    }
    #endregion
}
