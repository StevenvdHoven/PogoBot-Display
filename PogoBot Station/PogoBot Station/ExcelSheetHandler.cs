using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Download;
using Newtonsoft.Json;
using Google.Apis;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;

namespace PacoBot_Station
{
    public class ExcelSheetHandler
    {
        public const string SheetId = "14q8_QegkGyfBR5X_9tXcFoVE-4qymxmOYiESmFP6UgM";
        public static readonly string sheet = "Per User Logs";
        public static SheetsService Service;

        private static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private static string ApplicationName = "Google Sheets API .NET Quickstart";

        public static int Count { get; private set; }

        public static void ConnectSheet()
        {
            UserCredential credential;
            if (File.Exists("Accounts_data.json"))
            {
                ExcelData _data = LoadData();
                Count = _data.Count;
            }
            else
            {
                SaveData();
                ExcelData _data = LoadData();
                Count = _data.Count;
            }
            using (FileStream stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            SheetsService service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            Service = service;

            List<ModLogSaveData> _accounts = ReadSheet();
            if (_accounts.Count < Count)
            {
                Count = _accounts.Count;
            }
        }

        public static List<ModLogSaveData> ReadSheet()
        {
            // Define request parameters.
            String spreadsheetId = SheetId;
            String range = $"{sheet}!A2:F{Count + 1}";
            if(Count == 0)
            {
                range = $"{sheet}!A2:F1000";
            }
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    Service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<object>> values = response.Values;

            List<ModLogSaveData> _data = new List<ModLogSaveData>();
            if (values != null && values.Count > 0)
            {
                int _index = 2;
                foreach (IList<object> row in values)
                {
                    if (row != null)
                    {
                        if ((string)row[0] != "")
                        {
                            string _username = row[0].ToString();
                            ulong _userId = Convert.ToUInt64((string)row[1]);
                            int _warningLevel = Convert.ToInt16(row[2]);
                            int _kicks = Convert.ToInt16(row[3]);
                            string _banned = row[4].ToString();
                            string _note = "none";
                            if (row.Count == 6)
                            {
                                _note = row[5].ToString();
                            }

                            ModLogSaveData _saveData = new ModLogSaveData
                            {
                                UserName = _username,
                                UserID = _userId,
                                WarningLevel = _warningLevel,
                                Kicks = _kicks,
                                Banned = _banned,
                                Note = _note,
                                Index = _index,
                            };
                            _index++;
                            _data.Add(_saveData);
                        }
                    }
                }
            }
            return _data;
        }

        public static void AddOrUpdate(ModLogSaveData _data)
        {
            List<ModLogSaveData> _accounts = ReadSheet();
            bool update = false;
            int index = Count;
            for (int i = 0; i < _accounts.Count; i++)
            {
                if (_accounts[i].UserID == _data.UserID)
                {
                    ModLogSaveData _saveData = _accounts[i];
                    _saveData.WarningLevel = _saveData.WarningLevel + 1;
                    if (_data.Note != " " || _data.Note != "")
                    {
                        _saveData.Note += " : " + _data.Note;
                    }
                    index = _saveData.Index;
                    _data = _saveData;
                    update = true;
                }
            }

            if (update)
            {
                string range = $"{sheet}!A{index}:F";
                var valuerange = new ValueRange();
                List<object> _objectlist = new List<object>();
                _objectlist.Add(_data.UserName);
                _objectlist.Add(_data.UserID.ToString());
                _objectlist.Add(_data.WarningLevel);
                _objectlist.Add(_data.Kicks);
                _objectlist.Add(_data.Banned);
                _objectlist.Add(_data.Note);

                valuerange.Values = new List<IList<object>> { _objectlist };

                SpreadsheetsResource.ValuesResource.UpdateRequest updateReqeust = Service.Spreadsheets.Values.Update(valuerange, SheetId, range);
                updateReqeust.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                UpdateValuesResponse appendresponse = updateReqeust.Execute();
            }
            else
            {
                string range = $"{sheet}!A{index}:F";
                var valuerange = new ValueRange();
                List<object> _objectlist = new List<object>();
                _objectlist.Add(_data.UserName);
                _objectlist.Add(_data.UserID.ToString());
                _objectlist.Add(_data.WarningLevel);
                _objectlist.Add(_data.Kicks);
                _objectlist.Add(_data.Banned);
                _objectlist.Add(_data.Note);

                valuerange.Values = new List<IList<object>> { _objectlist };

                SpreadsheetsResource.ValuesResource.AppendRequest appendReqeust = Service.Spreadsheets.Values.Append(valuerange, SheetId, range);
                appendReqeust.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                AppendValuesResponse appendresponse = appendReqeust.Execute();
                Count++;
            }
            SaveData();
        }

        public static void SaveData()
        {
            using (StreamWriter stream = File.CreateText("Accounts_data.json"))
            {
                List<ModLogSaveData> _accounts = ReadSheet();
                ExcelData excelData = new ExcelData
                {
                    Count = Count,
                    CharLastAxis = "F",
                    Accounts = _accounts,
                };
                string _jsonstring = JsonConvert.SerializeObject(excelData);
                stream.Write(_jsonstring);
                stream.Close();
            }
        }

        public static ExcelData LoadData()
        {
            using (StreamReader stream = File.OpenText("Accounts_data.json"))
            {
                string _ReadedEnd = stream.ReadToEnd();
                ExcelData excelData = JsonConvert.DeserializeObject<ExcelData>(_ReadedEnd);

                stream.Close();
                return excelData;
            }
        }

        [Serializable]
        public struct ExcelData
        {
            public int Count;
            public string CharLastAxis;
            public List<ModLogSaveData> Accounts;
        }

        [Serializable]
        public struct ModLogSaveData
        {
            public string UserName;
            public ulong UserID;
            public int WarningLevel;
            public int Kicks;
            public string Banned;
            public string Note;

            public int Index;
        }
    }
}