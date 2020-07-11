using System.Collections;
using System.Collections.Generic;
using UnityEngine;

sealed public class Enemy : MonoBehaviour
{
    // Limit positions
    private GameObject[] limitPositions;

    // Random move timer
    private float movementDelay;
    private float movementCounter;
    private float positionX;
    private float positionZ;
    private bool movingToNewPos;

    // Hit
    public bool CanBeHit { get; set; }
    public bool Damaged { get; set; }

    // Movement
    float moveX;
    float moveZ;
    private bool pressingWalk;
    private Vector3 currentVelocity;
    [SerializeField] private float speed;

    // Ground Check
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private LayerMask groundLayer;

    // Jump
    [SerializeField] private float jumpForce;
    private float movementBeforeJumping;

    // Attack
    private bool canMeleeOrJump;
    private bool meleePunch;
    private bool meleeKick;
    private bool airMeleeAttack;
    [SerializeField] private Transform meleePosition;
    [SerializeField] private Vector3 meleePunchRadius;
    [SerializeField] private Vector3 meleeKickRadius;
    [SerializeField] private Vector3 meleeAirKickRadius;

    // Player
    [SerializeField] private LayerMask playerLayer;
    private Player p1;

    // Enemy Controls
    private bool ctrlPunch;
    private bool ctrlKick;
    private bool ctrlMoveX;
    private bool ctrlMoveZUp;
    private bool ctrlMoveZDown;
    private bool ctrlAirAttack;
    private bool ctrlJump;

    // Components
    public  Rigidbody rb { get; set; }
    private Animator anim;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        p1 = FindObjectOfType<Player>();

        canMeleeOrJump = true;

        CanBeHit    = true;
        Damaged     = false;

        ctrlPunch   = false;
        ctrlKick    = false;
        ctrlMoveX   = false;
        ctrlMoveZUp   = false;
        ctrlMoveZDown = false;
        ctrlJump    = false;

        limitPositions = new GameObject[3];
        limitPositions[0] = GameObject.FindGameObjectWithTag("maxLeftTop");
        limitPositions[1] = GameObject.FindGameObjectWithTag("maxRight");
        limitPositions[2] = GameObject.FindGameObjectWithTag("maxBottom");

        movementDelay = 1f;
        movementCounter = movementDelay;
        movingToNewPos = false;
    }

    // Update is called once per frame
    private void Update()
    {
        if (p1 == null) p1 = FindObjectOfType<Player>();    // Finds player

        Hit();
        GroundCheck();
        AttackConditions();
        Jump();
        rb.velocity = currentVelocity;
        SpriteRotation();
        Animations();


        if (Damaged == false)
        {
            if (movementCounter < 0)
            {
                positionX = Random.Range(limitPositions[0].transform.position.x, limitPositions[1].transform.position.x);
                positionZ = Random.Range(limitPositions[0].transform.position.z, limitPositions[2].transform.position.z);
                movementCounter = movementDelay;
                movingToNewPos = true;
            }
            if (movingToNewPos)
            {
                pressingWalk = true;
                Vector3 newPosition = default;
                if (transform.position != new Vector3(positionX, 0f, positionZ))
                {
                    newPosition = Vector3.MoveTowards(transform.position, new Vector3(positionX, 0f, positionZ), (speed / 1.5f) * Time.deltaTime);
                }
                transform.position = newPosition;

                if (transform.position == new Vector3(positionX, 0f, positionZ))
                {
                    pressingWalk = false;
                    movingToNewPos = false;
                }
            }
            else
            {
                movementCounter -= Time.deltaTime;
            }
        }
    }

    private void AttackConditions()
    {
        // Punch
        if (GroundCheck() && ctrlPunch && canMeleeOrJump) meleePunch = true;       // punch animation true
        else meleePunch = false;

        // Kick
        if (GroundCheck() && ctrlKick && canMeleeOrJump) meleeKick = true;        // kick animation true
        else meleeKick = false;

        // Air attack
        if (!GroundCheck() && (ctrlPunch || ctrlKick)) airMeleeAttack = true;  // air attack animation true
        else if (GroundCheck()) airMeleeAttack = false;
        if (airMeleeAttack) MeleeAttack();
    }

    private void Hit()
    {
        currentVelocity = rb.velocity;

        if (GroundCheck()) CanBeHit = true;
        if (CanBeHit)
        {
            if (Damaged)
            {
                movingToNewPos = false;
                pressingWalk = false;

                currentVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
                CanBeHit = false;
                Damaged = false;
            }
            else
            {
                currentVelocity = new Vector3(currentVelocity.x, currentVelocity.y, currentVelocity.z);
            }
        }
    }

    private bool GroundCheck()
    {
        // Checks if the player is grounded
        if (Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Jump()
    {
        // only jumps if the player isn't atacking
        if (canMeleeOrJump)
        {
            if (GroundCheck())
            {
                if (ctrlJump)
                {
                    movementBeforeJumping = moveX; // Saves the movement the player had before jumping
                    currentVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
                    ctrlJump = false;
                }
            }
            else if (GroundCheck() == false)
            {
                // Gives the translation the player had before jumping
                transform.Translate(new Vector3(movementBeforeJumping, 0f, 0f).normalized * speed * Time.deltaTime);
            }
        }

    }

    private void Animations()
    {
        anim.SetBool("pressingWalk", pressingWalk);
        anim.SetFloat("velocityY", currentVelocity.y);
        anim.SetBool("pressedJump", ctrlJump && canMeleeOrJump);
        anim.SetBool("canMeleeOrJump", canMeleeOrJump);
        anim.SetBool("onGround", GroundCheck());
        anim.SetBool("meleePunch", meleePunch);
        anim.SetBool("meleeKick", meleeKick);
        anim.SetBool("airMeleeAttack", airMeleeAttack);
        anim.SetBool("damaged", Damaged);
    }

    private void SpriteRotation()
    {
        // Only rotates if the player is grounded
        if (GroundCheck())
        {
            // If velocity is negative and the sprite is positive, rotates the sprite to the left
            if ( p1.transform.position.x < transform.position.x)
                if (transform.right.x > 0)
                    transform.rotation = Quaternion.Euler(0, 180, 0);
            // Else, rotates it back to the original position
            if (p1.transform.position.x > transform.position.x)
                if (transform.right.x < 0)
                    transform.rotation = Quaternion.identity;
        }
    }

    private void MeleeAttack()
    {
        Collider[] playerHit = null;

        if (meleePunch)
            playerHit = Physics.OverlapBox(meleePosition.position, meleePunchRadius, Quaternion.identity, playerLayer);

        if (meleeKick)
            playerHit = Physics.OverlapBox(meleePosition.position, meleeKickRadius, Quaternion.identity, playerLayer);

        if (airMeleeAttack)
            playerHit = Physics.OverlapBox(meleePosition.position, meleeAirKickRadius, Quaternion.identity, playerLayer);

        if (playerHit != null)
        {
            foreach (Collider player in playerHit)
            {
                if (airMeleeAttack && player.GetComponent<Player>().CanBeHit) PushEnemy(player.GetComponent<Player>());

                Player hitPlayer = player.GetComponent<Player>();
                hitPlayer.Damaged = true;
            }
        }
    }
    private void StartMeleeAttackDelay() { canMeleeOrJump = false; }
    private void RefreshMeleeAttackDelay() { canMeleeOrJump = true; }

    private void PushEnemy(Player player)
    {
        if (transform.position.x < player.transform.position.x) player.rb.AddForce(150f, 150f, 0f);
        else if (transform.position.x > player.transform.position.x) player.rb.AddForce(-150f, 150f, 0f);
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        // punch
        Gizmos.color = new Color(1, 0, 0);
        if (transform.right.x > 0)
            Gizmos.DrawWireCube(new Vector3(meleePosition.position.x + meleePunchRadius.x / 2, meleePosition.position.y, meleePosition.position.z), meleePunchRadius);
        else
            Gizmos.DrawWireCube(new Vector3(meleePosition.position.x - meleePunchRadius.x / 2, meleePosition.position.y, meleePosition.position.z), meleePunchRadius);

        // kick
        Gizmos.color = new Color(0, 1, 0);
        if (transform.right.x > 0)
            Gizmos.DrawWireCube(new Vector3(meleePosition.position.x + meleeKickRadius.x / 2, meleePosition.position.y, meleePosition.position.z), meleeKickRadius);
        else
            Gizmos.DrawWireCube(new Vector3(meleePosition.position.x - meleeKickRadius.x / 2, meleePosition.position.y, meleePosition.position.z), meleeKickRadius);

        // air kick
        Gizmos.color = new Color(0, 0, 1);
        if (transform.right.x > 0)
            Gizmos.DrawWireCube(new Vector3(meleePosition.position.x + meleeAirKickRadius.x / 2, meleePosition.position.y, meleePosition.position.z), meleeAirKickRadius);
        else
            Gizmos.DrawWireCube(new Vector3(meleePosition.position.x - meleeAirKickRadius.x / 2, meleePosition.position.y, meleePosition.position.z), meleeAirKickRadius);
    }
}
