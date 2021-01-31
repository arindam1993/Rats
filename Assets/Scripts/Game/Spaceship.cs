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
using UnityEngine.Experimental.Rendering.Universal;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Photon.Pun.Demo.Asteroids
{
    public class Spaceship : MonoBehaviour
    {
        public float MovementSpeed;

        public GameObject BulletPrefab;

        private PhotonView photonView;

        private new Rigidbody2D rigidbody;
        private new Collider collider;

        private Vector3 moveDirection = Vector3.up;
        private Vector3 mousePosition = Vector3.zero;
        private Plane gamePlane = new Plane(new Vector3(0, 0, -1), new Vector3(0, 0, 0));
        private float shootingTimer = 0.0f;

        private Animator animator;
        private float cameraZOffset = -100.0f;

        private Vector3 lastPosition;

        private bool controllable = true;


        private Light2D Flashlight;
        private Light2D Headlight;
        private int lightLayer;
        private int obstacleLayer;
        private int playerLayer;

        #region UNITY

        public void Awake()
        {
            photonView = GetComponent<PhotonView>();

            rigidbody = GetComponent<Rigidbody2D>();
            collider = GetComponent<Collider>();

            lightLayer = 1 << LayerMask.NameToLayer("Light");
            obstacleLayer = 1 << LayerMask.NameToLayer("Obstacle");
            playerLayer = 1 << LayerMask.NameToLayer("Player");

        }

        public void Start()
        {

            Transform renderObj = transform.Find("RenderObj");
            animator = renderObj.GetComponent<Animator>();

            Renderer r = renderObj.GetComponent<Renderer>();

            Flashlight = transform.Find("Flashlight").GetComponent<Light2D>();
            Headlight = transform.Find("Headlight").GetComponent<Light2D>();

            if (photonView.IsMine)
            {
                Headlight.intensity = 1.0f;
                Flashlight.intensity = 1.0f;
            } else
            {
                AsteroidsGameManager.Instance.AddNotMySpaceship(this);
            }

            lastPosition = transform.position;
            r.material.SetColor("Base Map", AsteroidsGame.GetColor(photonView.Owner.GetPlayerNumber()));
            StartCoroutine(AnimationWatcher());
        }

        public void Update()
        {
            if (!photonView.IsMine || !controllable)
            {
                return;
            }

            UpdateVisibleLights();

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


        private void UpdateVisibleLights()
        {
            // Find all lights in a circluar range
            //Collider2D[] inRange = Physics2D.OverlapCircleAll(transform.position, 10.0f, lightLayer);
            foreach (Spaceship other in AsteroidsGameManager.Instance.notMySpaceships)
            {
                other.Flashlight.enabled = false;
            }

            foreach (Spaceship other in AsteroidsGameManager.Instance.notMySpaceships)
            {
                if(Vector3.Distance(transform.position, other.transform.position) < 20.0f)
                {

                    Debug.DrawLine(transform.position, other.transform.position, Color.red, 0.1f, false);
                    if (IsEnemyLightVisible(other))
                    {
                        other.Flashlight.enabled = true;
                        other.Flashlight.intensity = 1.0f;
                    }
                }
            }

            /*
            foreach(Collider2D c in inRange)
            {
                GameObject playerGO = c.transform.parent.gameObject;
                Spaceship player = playerGO.GetComponent<Spaceship>();
                if (player && player != this)
                {
                    Debug.DrawLine(transform.position, player.transform.position, Color.red, 0.1f, false);
                    if (IsEnemyLightVisible(player))
                    {
                        player.Flashlight.enabled = true;
                        player.Flashlight.intensity = 1.0f;
                    }
                }
            }
            */

        }

        private bool IsEnemyLightVisible(Spaceship player)
        {
            // if I have line of sight to the enemy then i definitely hcan see the light
            // if (!Physics2D.Linecast(transform.position, player.transform.position, obstacleLayer)) return true;

            //Sweep the light cone
            Vector3 origin = player.transform.position;
            int numSteps = 50;
            float lightAngle = player.Flashlight.pointLightOuterAngle;
            float lightRadius = player.Flashlight.pointLightOuterRadius;
            float angleStep = lightAngle / numSteps;

            Vector3 leftEdge = Quaternion.AngleAxis(lightAngle / 2, new Vector3(0, 0, -1)) * (player.transform.up * lightRadius);

            for(int i = 0; i < numSteps; i++)
            {
                float currAngle = i * angleStep;
                Vector3 conePt = origin + Quaternion.AngleAxis(-currAngle, new Vector3(0, 0, -1)) * leftEdge;
                RaycastHit2D hit = Physics2D.Linecast(origin, conePt, obstacleLayer | playerLayer);
                Vector3 litHitPt = hit ? toVec3(hit.point + hit.normal * 0.0001f) : conePt;
                RaycastHit2D lightHit = Physics2D.Linecast(transform.position, litHitPt, obstacleLayer | playerLayer);
                Debug.DrawLine(origin, litHitPt , Color.green, 0.1f, false);
                Debug.DrawLine(transform.position, litHitPt , !lightHit ? Color.blue : Color.yellow, 0.1f, false);


                if (!lightHit) return true;
            }

            return false;
        }

        Vector3 toVec3(Vector2 v) {
            return new Vector3(v.x, v.y, 0);
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

            rigidbody.MovePosition(transform.position + moveDirection * MovementSpeed * Time.fixedDeltaTime);

            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, cameraZOffset);

        }

        #endregion

        #region COROUTINES

        private IEnumerator WaitForRespawn()
        {
            yield return new WaitForSeconds(AsteroidsGame.PLAYER_RESPAWN_TIME);

            photonView.RPC("RespawnSpaceship", RpcTarget.AllViaServer);
        }

        private IEnumerator AnimationWatcher()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                float animationSpeed = (transform.position - lastPosition).magnitude * Time.deltaTime;
                animator.SetBool("isRunning", animationSpeed * 10000.0f > 0.0f);
                lastPosition = transform.position;
            }
        }

        #endregion

        #region PUN CALLBACKS

        [PunRPC]
        public void DestroySpaceship()
        {
            rigidbody.velocity = Vector3.zero;

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