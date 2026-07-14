using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Omnilatent.TutorialMaker
{
    public class TutorialManager : MonoBehaviour
    {
        static bool isShowing; //is a tutorial showing?
        public static TutorialProgress data;
        public static Action<TutorialData> onCompleteTutorial;

        /// <summary>
        /// Temporary store data about player's tutorial progress
        /// </summary>
        [Obsolete("No longer used internally. Now aliases data.finishedTutorials so tutorial progress has a single source of truth and manual UndoTutorial/SetTutorialSeeTime take effect immediately. Prefer HasSeenTutorial/SetTutorialSeeTime instead.")]
        public static Dictionary<string, int> cacheHasSeenTutorial => data.finishedTutorials;
        public static Dictionary<string, ITutorialDisplay> activeTutorials = new Dictionary<string, ITutorialDisplay>(); //store list of active tutorial to iterate through

        static TutorialManager()
        {
            Load();
        }

        private static void Load()
        {
            var textData = PlayerPrefs.GetString("TUTORIAL_PROGRESS", string.Empty);

            if (!string.IsNullOrEmpty(textData))
            {
                data = LitJson.JsonMapper.ToObject<TutorialProgress>(textData);
            }
            else
            {
                ResetData();
            }
        }

        public static void ResetData()
        {
            data = new TutorialProgress();
            data.Init();
            //activeTutorials.Clear();
            Save();
        }

        public static void Save()
        {
            var textData = LitJson.JsonMapper.ToJson(data);

            PlayerPrefs.SetString("TUTORIAL_PROGRESS", textData);
            PlayerPrefs.Save();
        }

        public static bool HasSeenTutorial(TutorialData tutData)
        {
            return HasSeenTutorial(tutData.Id, tutData.repeatTimes);
        }

        public static bool HasSeenTutorial(string id, int seeTime = 1)
        {
            int value = 0;
            if (data.finishedTutorials.ContainsKey(id)) value = data.finishedTutorials[id];
            return value >= seeTime;
        }

        public static bool CanShowTutorial(TutorialData tutData)
        {
            //Check if a tutorial is already being shown
            if (isShowing)
            {
                Debug.LogWarning("A tutorial is already being shown");
                return false;
            }

            //Check if all required steps have been completed
            bool seenAllRequireTut = true;
            for (int i = 0; i < tutData.requireTutorials.Count; i++)
            {
                if (!HasSeenTutorial(tutData.requireTutorials[i].tutorialData))
                {
                    seenAllRequireTut = false;
                    break;
                }
            }
            if (!seenAllRequireTut)
            {
                //("has not seen all required tut");
                return false;
            }

            //Check if this tutorial is already completed
            if (!HasSeenTutorial(tutData))
            {
                return true;
            }
            else return false;
        }

        public static void CompleteTutorial(TutorialData tutData, int seeTime = 1)
        {
            if (!HasSeenTutorial(tutData))
            {
                if (!data.finishedTutorials.ContainsKey(tutData.Id))
                    data.finishedTutorials.Add(tutData.Id, 0);
                data.finishedTutorials[tutData.Id] += 1;
                TutorialManager.activeTutorials.Remove(tutData.Id);

                if (tutData.saveOnDone) TutorialManager.Save();
                isShowing = false;
                onCompleteTutorial?.Invoke(tutData);
            }
        }

        public static void OnTutorialEndUnexpected()
        {
            isShowing = false;
        }

        public static void OnShowTutorial(TutorialData m_Data, ITutorialDisplay tutorialDisplay)
        {
            isShowing = true;
            if (activeTutorials.ContainsKey(m_Data.Id) && activeTutorials[m_Data.Id] != null)
            {
                return;
                //.LogError("A tutorial like this already exist, this should not happen");
            }
            else
            {
                if (activeTutorials.ContainsKey(m_Data.Id))
                    activeTutorials[m_Data.Id] = tutorialDisplay;
                else
                    activeTutorials.Add(m_Data.Id, tutorialDisplay);
            }
        }

        public static void UndoTutorial(TutorialData tutData)
        {
            if (!data.finishedTutorials.ContainsKey(tutData.Id)) return;
            SetTutorialSeeTime(tutData, 0);
        }
        
        public static void SetTutorialSeeTime(TutorialData tutData, int seeTime)
        {
            data.finishedTutorials[tutData.Id] = seeTime;
        }
    }
}