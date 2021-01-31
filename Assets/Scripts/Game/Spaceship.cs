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

    public class Spaceship : MonoBehaviour, IPunObservable
    {
        public float MovementSpeed;
        public float fov;
        public bool IsFlashlightOn;
        public Vector3 KnockbackVelocity = new Vector3(0,0,0);

        public GameObject BulletPrefab;

        private PhotonView photonView;

        private new Rigidbody2D rigidbody;
        private new Collider collider;

        private Vector3 moveDirection = Vector3.up;
        private Vector3 mousePosition = Vector3.zero;
        private Plane gamePlane = new Plane(new Vector3(0, 0, -1), new Vector3(0, 0, 0));

        public float AttackCooldown;
        public float AttackRadius;
        public float AttackIntensity;
        private float attackTimer = 0.0f;

        private Animator animator;
        private float cameraZOffset = -100.0f;

        private Vector3 lastPosition;

        private bool controllable = true;


        private Light2D Flashlight;
        private Collider2D FlashlightTrigger;
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
            FlashlightTrigger = transform.Find("Flashlight").GetComponent<Collider2D>();
            Headlight = transform.Find("Headlight").GetComponent<Light2D>();

            if (photonView.IsMine)
            {
                Headlight.intensity = 1.0f;
                Flashlight.intensity = 1.0f;
                FlashlightTrigger.enabled = true;
                IsFlashlightOn = true;
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

            if (Input.GetButtonUp("Jump"))
            {
                IsFlashlightOn = !IsFlashlightOn;
                Flashlight.enabled = IsFlashlightOn;
            }

            if (Input.GetMouseButtonDown(0) && attackTimer <= 0.0f) {
                photonView.RPC("Fire", RpcTarget.AllViaServer);
                attackTimer = AttackCooldown;
            }

            attackTimer -= Time.deltaTime;
        }


        private void UpdateVisibleLights()
        {
            foreach (Spaceship other in AsteroidsGameManager.Instance.notMySpaceships)
            {
                other.Flashlight.enabled = false;
            }

            foreach (Spaceship other in AsteroidsGameManager.Instance.notMySpaceships)
            {
                if(Vector3.Distance(transform.position, other.transform.position) < 20.0f)
                {

                    Debug.DrawLine(transform.position, other.transform.position, Color.red, 0.1f, false);
                    if (other.IsFlashlightOn && IsEnemyLightVisible(other))
                    {
                        other.Flashlight.enabled = true;
                        other.Flashlight.intensity = 1.0f;
                    }
                }
            }
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

                if (IsLightRayVisible(origin, litHitPt)) return true;
            }

            return false;
        }

        private bool IsLightRayVisible(Vector3 start, Vector3 end)
        {
            int numSteps = 20;
            Vector3 t = end - start;
            for(int i = 0; i < numSteps; i++)
            {
                Vector3 endPt = start + t * (i / numSteps);

                // Account for player field of view 
                float angle = Vector3.Angle((endPt - transform.position).normalized, transform.up);
                if(angle < fov / 2)
                {
                    if (!Physics2D.Linecast(transform.position, endPt, obstacleLayer)) return true;
                }
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

            // If being knocked back then only knockback
            if (KnockbackVelocity.magnitude > 0.001f)
            {
                rigidbody.MovePosition(transform.position + KnockbackVelocity * Time.fixedDeltaTime);
                //decay knockback veclocity
                KnockbackVelocity = KnockbackVelocity * 0.5f;
            } else
            {
                Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
                float t = 0.0f;
                if (gamePlane.Raycast(mouseRay, out t))
                {
                    Vector3 lookPos = mouseRay.GetPoint(t);
                    Vector3 lookDir = Vector3.Normalize(lookPos - transform.position);

                    transform.up = lookDir;
                }

                rigidbody.MovePosition(transform.position + moveDirection * MovementSpeed * Time.fixedDeltaTime);
            }
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, cameraZOffset);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!photonView.IsMine)
            {
                if (collision.gameObject.tag == "Light")
                {
                    Headlight.enabled = true;
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!photonView.IsMine)
            {
                if (collision.gameObject.tag == "Light")
                {
                    Headlight.enabled = false;
                }
            }
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
        public void Fire(PhotonMessageInfo info)
        {
            animator.SetTrigger("attack");

            if (photonView.IsMine)
            {
                Vector2 hitCenter = transform.position + transform.up * AttackRadius;
                Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, AttackRadius, playerLayer);

                foreach(Collider2D hit in hits)
                {
                    Spaceship player = hit.gameObject.GetComponent<Spaceship>();
                    if(player != this)
                    {
                        Vector3 hitDirection = Vector3.Normalize(player.transform.position - transform.position);
                        player.KnockbackVelocity = hitDirection * AttackIntensity;
                    }
                }
            }
        }

        [PunRPC]
        public void RespawnSpaceship()
        {
            collider.enabled = true;

            controllable = true;
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                //We own this player: send the others our data
                stream.SendNext(IsFlashlightOn);
                stream.SendNext(KnockbackVelocity);
            }
            else
            {
                //Network player, receive data
                IsFlashlightOn = (bool)stream.ReceiveNext();
                KnockbackVelocity = (Vector3)stream.ReceiveNext();
            }
        }

        #endregion
    }
}