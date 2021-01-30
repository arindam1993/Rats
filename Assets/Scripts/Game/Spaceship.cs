// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Spaceship.cs" company="Exit Games GmbH">
//   Part of: Asteroid Demo,
// </copyright>
// <summary>
//  Spaceship
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;

using UnityEngine;

using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Photon.Pun.Demo.Asteroids
{
    public class Spaceship : MonoBehaviour
    {
        public float MovementSpeed;

        public GameObject BulletPrefab;

        private PhotonView photonView;

        private new Rigidbody rigidbody;
        private new Collider collider;

        private Vector3 moveDirection = Vector3.up;
        private Vector3 mousePosition = Vector3.zero;
        private Plane gamePlane = new Plane(new Vector3(0, 0, -1), new Vector3(0, 0, 0));
        private float shootingTimer = 0.0f;

        private bool controllable = true;

        #region UNITY

        public void Awake()
        {
            photonView = GetComponent<PhotonView>();

            rigidbody = GetComponent<Rigidbody>();
            collider = GetComponent<Collider>();
        }

        public void Start()
        {

            Transform renderObj = transform.Find("RenderObj");
            Renderer r = renderObj.GetComponent<Renderer>();

            r.material.SetColor("Base Map", AsteroidsGame.GetColor(photonView.Owner.GetPlayerNumber()));    
        }

        public void Update()
        {
            if (!photonView.IsMine || !controllable)
            {
                return;
            }

            moveDirection = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);
            mousePosition = Input.mousePosition;

            if (Input.GetButton("Jump") && shootingTimer <= 0.0)
            {
                shootingTimer = 0.2f;

                photonView.RPC("Fire", RpcTarget.AllViaServer, rigidbody.position, rigidbody.rotation);
            }

            if (shootingTimer > 0.0f)
            {
                shootingTimer -= Time.deltaTime;
            }
        }

        public void FixedUpdate()
        {
            if (!photonView.IsMine)
            {
                return;
            }

            if (!controllable)
            {
                return;
            }

            Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
            float t = 0.0f;
            if (gamePlane.Raycast(mouseRay,out t))
            {
                Vector3 lookPos = mouseRay.GetPoint(t);
                Vector3 lookDir = Vector3.Normalize(lookPos - transform.position);

                transform.up = lookDir;
            }

            transform.position += moveDirection * MovementSpeed * Time.fixedDeltaTime;

        }

        #endregion

        #region COROUTINES

        private IEnumerator WaitForRespawn()
        {
            yield return new WaitForSeconds(AsteroidsGame.PLAYER_RESPAWN_TIME);

            photonView.RPC("RespawnSpaceship", RpcTarget.AllViaServer);
        }

        #endregion

        #region PUN CALLBACKS

        [PunRPC]
        public void DestroySpaceship()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            collider.enabled = false;

            controllable = false;


            if (photonView.IsMine)
            {
                object lives;
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(AsteroidsGame.PLAYER_LIVES, out lives))
                {
                    PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable {{AsteroidsGame.PLAYER_LIVES, ((int) lives <= 1) ? 0 : ((int) lives - 1)}});

                    if (((int) lives) > 1)
                    {
                        StartCoroutine("WaitForRespawn");
                    }
                }
            }
        }

        [PunRPC]
        public void Fire(Vector3 position, Quaternion rotation, PhotonMessageInfo info)
        {
            float lag = (float) (PhotonNetwork.Time - info.SentServerTime);
            GameObject bullet;

            /** Use this if you want to fire one bullet at a time **/
            bullet = Instantiate(BulletPrefab, rigidbody.position, Quaternion.identity) as GameObject;
            bullet.GetComponent<Bullet>().InitializeBullet(photonView.Owner, (rotation * Vector3.forward), Mathf.Abs(lag));


            /** Use this if you want to fire two bullets at once **/
            //Vector3 baseX = rotation * Vector3.right;
            //Vector3 baseZ = rotation * Vector3.forward;

            //Vector3 offsetLeft = -1.5f * baseX - 0.5f * baseZ;
            //Vector3 offsetRight = 1.5f * baseX - 0.5f * baseZ;

            //bullet = Instantiate(BulletPrefab, rigidbody.position + offsetLeft, Quaternion.identity) as GameObject;
            //bullet.GetComponent<Bullet>().InitializeBullet(photonView.Owner, baseZ, Mathf.Abs(lag));
            //bullet = Instantiate(BulletPrefab, rigidbody.position + offsetRight, Quaternion.identity) as GameObject;
            //bullet.GetComponent<Bullet>().InitializeBullet(photonView.Owner, baseZ, Mathf.Abs(lag));
        }

        [PunRPC]
        public void RespawnSpaceship()
        {
            collider.enabled = true;

            controllable = true;
        }
        
        #endregion
    }
}