// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidsGameManager.cs" company="Exit Games GmbH">
//   Part of: Asteroid demo
// </copyright>
// <summary>
//  Game Manager for the Asteroid Demo
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Photon.Pun.Demo.Asteroids
{
    public class AsteroidsGameManager : MonoBehaviourPunCallbacks
    {
        public static AsteroidsGameManager Instance = null;

        public Text InfoText;

        public GameObject[] AsteroidPrefabs;

        private GameObject SpawnPositions;

        public List<Spaceship> notMySpaceships;

        #region UNITY

        public void Awake()
        {
            Instance = this;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
        }

        public void Start()
        {
            InfoText.text = "Waiting for other players...";

            Hashtable props = new Hashtable
            {
                {AsteroidsGame.PLAYER_LOADED_LEVEL, true}
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;
        }

        #endregion

        public void AddNotMySpaceship(Spaceship s)
        {
            if(notMySpaceships == null)
            {
                notMySpaceships = new List<Spaceship>();
            }

            notMySpaceships.Add(s);
        }

        #region COROUTINES


        private IEnumerator EndOfGame(string winner, int score)
        {
            float timer = 5.0f;

            while (timer > 0.0f)
            {
                InfoText.text = string.Format("Player {0} won with {1} points.\n\n\nReturning to login screen in {2} seconds.", winner, score, timer.ToString("n2"));

                yield return new WaitForEndOfFrame();

                timer -= Time.deltaTime;
            }

            PhotonNetwork.LeaveRoom();
        }

        #endregion

        #region PUN CALLBACKS

        public override void OnDisconnected(DisconnectCause cause)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.Disconnect();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            CheckEndOfGame();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(AsteroidsGame.PLAYER_LIVES))
            {
                CheckEndOfGame();
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            if (changedProps.ContainsKey(AsteroidsGame.PLAYER_LOADED_LEVEL))
            {
                if (CheckAllPlayerLoadedLevel())
                {
                    Hashtable props = new Hashtable
                    {
                        {CountdownTimer.CountdownStartTime, (float) PhotonNetwork.Time}
                    };
                    PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                }
            }
        }

        #endregion

        private void StartGame()
        {
            SpawnPositions = GameObject.Find("SpawnPositions");
            int playerIdx = PhotonNetwork.LocalPlayer.GetPlayerNumber();
            Vector3 position = SpawnPositions.GetComponentsInChildren<Transform>()[playerIdx].position;

            PhotonNetwork.SendRate = 60;
            PhotonNetwork.SerializationRate = 60;

            PhotonNetwork.Instantiate("Spaceship", position, Quaternion.identity, 0);

        }

        private bool CheckAllPlayerLoadedLevel()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object playerLoadedLevel;

                if (p.CustomProperties.TryGetValue(AsteroidsGame.PLAYER_LOADED_LEVEL, out playerLoadedLevel))
                {
                    if ((bool) playerLoadedLevel)
                    {
                        continue;
                    }
                }

                return false;
            }

            return true;
        }

        private void CheckEndOfGame()
        {
            int numAlive = PhotonNetwork.PlayerList.Length;
            Player lastPlayerAlive = null;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object lives;
                if (p.CustomProperties.TryGetValue(AsteroidsGame.PLAYER_LIVES, out lives))
                {
                    if ((int) lives <= 0)
                    {
                        numAlive -= 1;
                    }
                    else
                    {
                        lastPlayerAlive = p;
                    }
                }
            }

            if (numAlive == 1)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    StopAllCoroutines();
                }

                string winner = lastPlayerAlive.NickName;
                int score = lastPlayerAlive.GetScore();

                StartCoroutine(EndOfGame(winner, score));
            }
        }

        private void OnCountdownTimerIsExpired()
        {
            StartGame();
        }
    }
}