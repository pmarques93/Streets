using System.Collections;
using System.Collections.Generic;
using UnityEngine;

sealed public class Player : MonoBehaviour
{
    // Hit
    public bool CanBeHit { get; set; }
    public bool Damaged { get; set; }

    // Movement
    float moveX;
    float moveZ;
    private bool    pressingWalk;
    private Vector3 currentVelocity;
    [SerializeField] private float speed;

    // Ground Check
    [SerializeField] private Transform  groundCheck;
    [SerializeField] private float      groundCheckRadius;
    [SerializeField] private LayerMask  groundLayer;

    // Jump
    [SerializeField] private float  jumpForce;
    private float   movementBeforeJumping;

    // Attack
    private bool canMeleeOrJump;
    private bool meleePunch;
    private bool meleeKick;
    private bool airMeleeAttack;
    [SerializeField] private Transform  meleePosition;
    [SerializeField] private Vector3    meleePunchRadius;
    [SerializeField] private Vector3    meleeKickRadius;
    [SerializeField] private Vector3    meleeAirKickRadius;

    // Enemy
    [SerializeField] private LayerMask enemyLayer;

    // Components
    public  Rigidbody   rb { get; set; }
    private Animator    anim;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        canMeleeOrJump  = true;
    }

    // Update is called once per frame
    private void Update()
    {
        Hit();
        Movement();
        GroundCheck();
        AttackConditions();
        Jump();        
        rb.velocity = currentVelocity;
        SpriteRotation();
        Animations();
    }

    private void AttackConditions()
    {
        // Punch
        if (GroundCheck() && Input.GetButtonDown("Fire1") && canMeleeOrJump) meleePunch = true;       // punch animation true
        else meleePunch = false;

        // Kick
        if (GroundCheck() && Input.GetButtonDown("Fire2") && canMeleeOrJump) meleeKick = true;        // kick animation true
        else meleeKick = false;

        // Air attack
        if (!GroundCheck() && (Input.GetButtonDown("Fire1") || Input.GetButtonDown("Fire2"))) airMeleeAttack = true;  // air attack animation true
        else if (GroundCheck()) airMeleeAttack = false;
        if (airMeleeAttack) MeleeAttack();
    }

    private void Hit()
    {
        if (GroundCheck()) CanBeHit = true;
        if (CanBeHit)
        {
            if (Damaged)
            {
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

    private void Movement()
    {
        currentVelocity = rb.velocity;
        pressingWalk = false;
        moveX = default;
        moveZ = default;

        // only moves if the player isn't atacking and the player is on the floor
        if (canMeleeOrJump)
        {
            if (GroundCheck())
            {
                // Left right
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
                {
                    pressingWalk = true;
                    moveX = 1;
                }

                // Up
                if (Input.GetKey(KeyCode.W) && transform.right.x > 0)
                {
                    pressingWalk = true;
                    moveZ = 1;
                }
                else if (Input.GetKey(KeyCode.W) && transform.right.x < 0)
                {
                    pressingWalk = true;
                    moveZ = -1;
                }
                // Down
                if (Input.GetKey(KeyCode.S) && transform.right.x > 0)
                {
                    pressingWalk = true;
                    moveZ = -1;
                }
                else if (Input.GetKey(KeyCode.S) && transform.right.x < 0)
                {
                    pressingWalk = true;
                    moveZ = 1;
                }
            }
        }

        transform.Translate(new Vector3(moveX, 0f, moveZ).normalized * speed * Time.deltaTime);
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
                if (Input.GetButtonDown("Jump"))
                {
                    movementBeforeJumping = moveX; // Saves the movement the player had before jumping
                    currentVelocity = new Vector3(currentVelocity.x, jumpForce, currentVelocity.z);
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
        anim.SetBool("pressedJump", Input.GetButtonDown("Jump") && canMeleeOrJump);
        anim.SetBool("canMeleeOrJump", canMeleeOrJump);
        anim.SetBool("onGround", GroundCheck());
        anim.SetBool("meleePunch", meleePunch);
        anim.SetBool("meleeKick", meleeKick);
        anim.SetBool("airMeleeAttack", airMeleeAttack);
    }

    private void SpriteRotation()
    {
        // Only rotates if the player is grounded
        if (GroundCheck())
        {
            // If velocity is negative and the sprite is positive, rotates the sprite to the left
            if (Input.GetKey(KeyCode.A))
                if (transform.right.x > 0)
                    transform.rotation = Quaternion.Euler(0, 180, 0);
            // Else, rotates it back to the original position
            if (Input.GetKey(KeyCode.D))
                if (transform.right.x < 0)
                    transform.rotation = Quaternion.identity;
        }
    }

    private void MeleeAttack()
    {
        Collider[] enemyHit = null;

        if (meleePunch)
            enemyHit = Physics.OverlapBox(meleePosition.position, meleePunchRadius, Quaternion.identity, enemyLayer);

        if (meleeKick)
            enemyHit = Physics.OverlapBox(meleePosition.position, meleeKickRadius, Quaternion.identity, enemyLayer);

        if (airMeleeAttack)
            enemyHit = Physics.OverlapBox(meleePosition.position, meleeAirKickRadius, Quaternion.identity, enemyLayer);

        if (enemyHit != null)
        {
            foreach (Collider enemy in enemyHit)
            {
                if (airMeleeAttack && enemy.GetComponent<Enemy>().CanBeHit) PushEnemy(enemy.GetComponent<Enemy>());

                Enemy hitEnemy = enemy.GetComponent<Enemy>();
                hitEnemy.Damaged = true;
            }
        }
    }
    private void StartMeleeAttackDelay()    { canMeleeOrJump = false; }
    private void RefreshMeleeAttackDelay()  { canMeleeOrJump = true;}
        
    private void PushEnemy(Enemy enemy)
    {
        if (transform.position.x < enemy.transform.position.x) enemy.rb.AddForce(150f, 150f, 0f);
        else if (transform.position.x > enemy.transform.position.x) enemy.rb.AddForce(-150f, 150f, 0f);
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
