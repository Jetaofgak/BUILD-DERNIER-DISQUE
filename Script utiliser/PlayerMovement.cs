using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    
    // 0,71 est le temps qu'il faut pour sauter
    // crée un systeme de fall damage.
    // crée un systheme de freemomentum avec un fonction
    public Rigidbody2D rbPlayer;

    // pour bouger et sauter
    [Header("Move and Jump")] 
    [SerializeField] float x; // Ici horizontal direction

    [SerializeField] public bool changingDirection => (rbPlayer.velocity.x > 0f && x < 0f) || (rbPlayer.velocity.x < 0f && x > 0f);
    [SerializeField] public float speed;
    [SerializeField] public float maxSpeed;
    [SerializeField] public float maxSpeedAir;
    [SerializeField] public bool freeMomentum = false;
    [SerializeField] public float acceleration;
    [SerializeField] public float linearDrag;
    [SerializeField] public float airLinearDrag = 2.5f;
    [SerializeField] public float jumpForce;
    [SerializeField] public float fallMultiplier;
    [SerializeField] public bool facingRight = true;

    [SerializeField] public float speedAir;
    

    // Les stock
    [Header("Stocks")] 
    [SerializeField] public int jumpStock = 0;
    [SerializeField] public int diskStock = 0;

    // Checker si sur ground.
    [Header("GroundSettings")] 
    [SerializeField] public bool isGrounded = false;
    [SerializeField] public Transform isGroundedChecker;
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public float range;

    //Checker si sur disque (Values)
    [Header("OnDiskSettings")] 
    [SerializeField] bool isOnCircle = false;
    [SerializeField] public Transform isOnCircleChecker;
    [SerializeField] public LayerMask diskLayer;


    // Pour les Disque
    [Header("GameObjects")] 
    [SerializeField] public GameObject circle;
    [SerializeField] public GameObject circleHori;
    [SerializeField] public GameObject circleVerti;
    [SerializeField] public float energyAbsorbed = 0f;
    // pour le side jump

    [Header("SideJumpSettings")]
    [SerializeField] public float jumpForceSide;
    [SerializeField] public float jumpForceSideY;

    // Pour le upJump
    [Header("UpJumpSettings")] 
    [SerializeField] public float jumpForceUp;
    [SerializeField] public JumpType jumpType;

    //Pour les detection de ceilling
    [Header("Plafond")]
    [SerializeField] bool isOnCeilling = false;
    [SerializeField] float timeOnCeilling = 0;
    [SerializeField] float rayRangeC = -1.9f;

    [Header("FallInteraction")]
    [SerializeField] public float fallTime = 0f;
    [SerializeField] public bool canTakeFallDamage = false;
    [SerializeField] public float timeToTakeFallDamage = 2;
    [SerializeField] public float FallDamageDamage = 1;

    public Coroutine crD;
    public CircleScript cS;

    public enum JumpType
    {
        None,
        JumpAirUpAndDirection,
        JumpUpAndDirection,
        JumpDirection,
        JumpNormal,
        JumpNormalAir,
        JumpAirSideLeft,
        JumpAirSideRight,
        JumpAirDown,
        JumpNeutral,
        JumpUpGround,
        JumpUp
    }

    public bool Space => Input.GetKeyDown(KeyCode.Space);
    public bool SpaceLeft => Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.LeftArrow);
    public bool SpaceRight => Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.RightArrow);
    public bool SpaceLeftOrRight => Input.GetKeyDown(KeyCode.Space) && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow));
    public bool SpaceUpAndLeftOrRight =>Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.UpArrow) && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow));
    public bool SpaceDown => Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.DownArrow);
    public bool SpaceUp => Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.UpArrow);
    

    
    
    void Start()
    {
        rbPlayer = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        //CheckIfOnCeilling();
        CheckIfGrounded();
        CheckIfOnCircle();
        InputJumps();
        GenerateCircle();
        FallDamageTester(); //canTakeFallDamage retourne true aprés un certain temps dans les airs.
        Test();
        
    }


    void FixedUpdate()
    {
        Jump();
        FallSpeed();
        Move();
        LimitSpeed();
        CeillingSlide();
        TimerCeilling();
    }
   
    public float StockEnergy(float velocitysent,float objectMass)
    {

        energyAbsorbed += (velocitysent*(objectMass*objectMass))/2;
        Debug.Log("Total energie : " + energyAbsorbed);

        return energyAbsorbed;
    }

    public void DischargeEnergy()
    {
        energyAbsorbed = 0;
    }
    private void FallDamageTester()
    {
        if(isGrounded == false)
        {
            fallTime = Time.deltaTime;
            if(fallTime > timeToTakeFallDamage)
            {
                canTakeFallDamage = true;
                
            }

        }
        else
        {
            fallTime = 0;
            canTakeFallDamage= false;
        }
    }
    public bool MFreeMomentum()
    {

        return freeMomentum;
    }
    private void LimitSpeed()
    {
        var currentMaxSpeed = float.PositiveInfinity;

        if (isGrounded)
            currentMaxSpeed = maxSpeed;
        else if (freeMomentum == false)
            currentMaxSpeed = maxSpeedAir;

        var velocityX = rbPlayer.velocity.x;

        if (Mathf.Abs(velocityX) > currentMaxSpeed)
            rbPlayer.velocity = new Vector2(Mathf.Sign(velocityX) * currentMaxSpeed, rbPlayer.velocity.y);
    }

    bool IsGroundedOrInOnCircle()
    {
        return isGrounded || isOnCircle;
    }

    void InputJumps()
    {
        if (IsGroundedOrInOnCircle())
        {
            //Input saut normal on ground avec une flèche. Ground
            if (SpaceLeftOrRight)
                jumpType = JumpType.JumpDirection;
            //Input Up jump Ground
            else if (SpaceUp)
                jumpType = JumpType.JumpUpGround;
            //Input normal jump
            else if (Space)
                jumpType = JumpType.JumpNormal;
        }
        else if (jumpStock > 0)
        {
            //Input saut normal avec 2 flèche Air.
            if (SpaceUpAndLeftOrRight)
                jumpType = JumpType.JumpAirUpAndDirection;
            //Input down jump
            else if (SpaceDown)
                jumpType = JumpType.JumpAirDown;
            //Input Side Right. Air
            else if (SpaceRight && x != 0)
                jumpType = JumpType.JumpAirSideRight;
            //Input Side Left. Air
            else if (SpaceLeft && x != 0)
                jumpType = JumpType.JumpAirSideLeft;
            //Input double jumps
            else if (Space)
                jumpType = JumpType.JumpNormalAir;
        }
    }

    void Test()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            freeMomentum = true;
            rbPlayer.AddForce(new Vector2(99990, 0), ForceMode2D.Force);
        }


       
            

      
    }

    void Move()
    {
        // partie freeze rota
        rbPlayer.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Partie mouvement
        x = Input.GetAxisRaw("Horizontal");
        float moveBy = x * speed;
        float moveByAir = x * speedAir;

        // Partie facing 
        if (isGrounded)
        {
            if (x > 0)
                facingRight = true;
            else if (x < 0)
                facingRight = false;
        }
        // partie down jump

        if (Input.GetKey(KeyCode.Space) && Input.GetKey(KeyCode.DownArrow) && (isGrounded || isOnCircle) &&
            diskStock > 0)
        {
            rbPlayer.velocity = Vector2.zero;
            return;
        }
        //Suite de partie mouvement

        if (isGrounded)
            rbPlayer.AddForce(new Vector2(moveBy, 0), ForceMode2D.Impulse);
        else if (isOnCeilling == false)
            rbPlayer.AddForce(new Vector2(moveByAir, 0), ForceMode2D.Impulse);
       
        

        


        else if (timeOnCeilling < 0 && isOnCeilling == false )
        {
            isOnCeilling = false;
            timeOnCeilling = 1.5f;
            GetComponent<Rigidbody2D>().gravityScale = 1.5f;

        }
       

    }

    public void ApplyLinearDrag()
    {
        if (Mathf.Abs(x) < 0.4f || changingDirection)
        {
            rbPlayer.drag = linearDrag;
        }
        else
        {
            rbPlayer.drag = 0f;
        }
    }

    public void ApplyAirLinearDrag()
    {
        rbPlayer.drag = airLinearDrag;

    }

    public void NoDrag()
    {
        
    }
    void FallSpeed()
    {
        if (rbPlayer.velocity.y < -0.4000000f)
        {
            var spd = Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
            rbPlayer.velocity += Vector2.up * spd;
        }
    }

    void Jump()
    {
        if (jumpType == JumpType.None) return;
        
        var velocity = new Vector2(rbPlayer.velocity.x, jumpForce);
        // Saut normal avec deux flèche. Air
        if (jumpType == JumpType.JumpAirUpAndDirection)
        {
            jumpStock = jumpStock - 1;
        }
        // saut normal avec deux flèche surle sol
        else if (jumpType == JumpType.JumpUpAndDirection)
        {
            rbPlayer.drag = airLinearDrag;
        }
        // saut normal on ground avec une flèche.
        else if (jumpType == JumpType.JumpDirection)
        {
            rbPlayer.drag = airLinearDrag;
        }
        // Down jump (Air) (Y revenir)
        else if (jumpType == JumpType.JumpAirDown)
        {
            velocity.y *= -1f;
            jumpStock = jumpStock - 1;
        }
        // UpJumpGround
        else if (jumpType == JumpType.JumpUpGround)
        {
            velocity.y = jumpForceUp;
        }
        // Side  right (Pour l'instant ça remet a 0,0 la velocity pour une frame. (Air)
        else if (jumpType == JumpType.JumpAirSideRight)
        {
            rbPlayer.velocity = Vector2.zero;
            rbPlayer.AddForce(new Vector2(jumpForceSide, jumpForceSideY), ForceMode2D.Impulse);
            jumpStock = jumpStock - 1;
            jumpType = JumpType.None;
            return;
        }
        //Side left (remet a 0,0 la velocity, mais pas sur que je garde) (Air)
        else if (jumpType == JumpType.JumpAirSideLeft)
        {
            rbPlayer.velocity = Vector2.zero;
            rbPlayer.AddForce(new Vector2(-jumpForceSide, jumpForceSideY), ForceMode2D.Impulse);
            jumpStock = jumpStock - 1;
            jumpType = JumpType.None;
            return;
        }
        // double saut (air)
        else if (jumpType == JumpType.JumpNormalAir)
        {
            jumpStock = jumpStock - 1;
        }
        // saut vertical neutral on ground.
        else if (jumpType == JumpType.JumpNormal)
        {
            rbPlayer.drag = airLinearDrag;
        }

        jumpType = JumpType.None;
        rbPlayer.velocity = velocity;
    }

    RaycastHit2D GetCollider(float offsetX, LayerMask layer, Color color)
    {
        Vector2 position = this.transform.position;
        position.x += offsetX;
        Vector2 direction = Vector2.down;
        RaycastHit2D collider = Physics2D.Raycast(position, direction, range, layer);
        Debug.DrawLine(position, position + direction * range, Color.red);
        return collider;
    }
    RaycastHit2D GetColliderUp(float offsetX, LayerMask layer, Color color)
    {
        Vector2 position = transform.position;
        position.x += offsetX;
        Vector2 direction = Vector2.down;
        RaycastHit2D collider = Physics2D.Raycast(position, direction, rayRangeC, layer);
        Debug.DrawLine(position, position + direction * rayRangeC, Color.red);
        return collider;
    }

    // ça utilise des raycast
    void CheckIfGrounded()
    {
        RaycastHit2D collider = GetCollider(0f, groundLayer, Color.green);
        RaycastHit2D colliderLD = GetCollider(-0.8f, groundLayer, Color.green);
        RaycastHit2D colliderRD = GetCollider(+0.8f, groundLayer, Color.green);

        isGrounded = collider.collider != null || colliderLD.collider != null || colliderRD.collider != null;

        if (isGrounded)
        {
            freeMomentum = false;
            jumpStock = 3;
            diskStock = 3;

            ApplyLinearDrag();
        }
        else
        {
            ApplyAirLinearDrag();
        }
    }
   
    void CheckIfOnCircle()
    {
        RaycastHit2D collider = GetCollider(0f, diskLayer, Color.red);
        RaycastHit2D colliderLD = GetCollider(-0.8f, diskLayer, Color.red);
        RaycastHit2D colliderRD = GetCollider(+0.8f, diskLayer, Color.red);

        isOnCircle = collider.collider != null || colliderLD.collider != null || colliderRD.collider != null;

        if (isOnCircle)
        {
            ApplyLinearDrag();
        }
    }

    void InstantiateCircle(GameObject prefab, float offsetX, float offsetY)
    {
        Vector3 transformPosition = transform.position;
        Vector2 position = new Vector2(transformPosition.x + offsetX, transformPosition.y + offsetY);
        Instantiate(prefab, position, Quaternion.identity);
    }
    
    void GenerateCircle()
    {
        //Generer un cercle avec les saut directionelle
        //Neutral Jump Avec Deux Fleches (Air)
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        
        if (IsGroundedOrInOnCircle())
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                //Un else if vide pour boucher un bug
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow))
                {
                    Debug.Log("CancelUJumpGround");
                }
                //Up jump ground
                else
                {
                    Debug.Log("UpJumpGround");
                    InstantiateCircle(circle, 1.5f, -2f);
                }
            }
            // Down jump ground
            else if (Input.GetKey(KeyCode.DownArrow) && diskStock > 0)
            {
                Debug.Log("DownJumpGround");
                InstantiateCircle(circleHori, 0f, 1.9f);
            }
        }
        else if (diskStock > 0)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow))
                {
                    // ne fait que générer un cercle. Same pour tout les autre.
                    Debug.Log("NjumpMod");
                    InstantiateCircle(circleHori, 0f, -2f);
                }
                // Up Jump air
                else
                {
                    Debug.Log("UpJumpAir");
                    InstantiateCircle(circle, 1.5f, -2f);
                }
            }
            // Down Jump air
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                freeMomentum = true;
                Debug.Log("DownJumpAir");
                InstantiateCircle(circleHori, 0f, +1.9f);
            }
            // Left Jump
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                freeMomentum = true;
                Debug.Log("LJump");
                InstantiateCircle(circleVerti, 1.7f, 0f);
            }
            // Right Jump
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                freeMomentum = true;
                Debug.Log("RJump");
                InstantiateCircle(circleVerti, -1.7f, 0f);
            }
            // Jump normal
            else
            {
                Debug.Log("Njump");
                InstantiateCircle(circleHori, 0f, -2f);
            }
            diskStock -= 1;
        }
    }

    

    // TOUT CE QU'IL Y A EN DESSOUS EST A REVOIR.
    private void OnCollisionEnter2D(Collision2D col)
    {
        float veloEnterY = col.relativeVelocity.y;
        



        if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (this.transform.position.y < col.contacts[0].point.y && diskStock > 0)
            {

                timeOnCeilling = Mathf.Sqrt(veloEnterY * veloEnterY) / 50 ;
                rbPlayer.gravityScale = 0;
            }

            

        }
            


    }

   void TimerCeilling()
    {
        
        if (timeOnCeilling >0)
            timeOnCeilling -= Time.deltaTime;
        if (timeOnCeilling <= 0)
            rbPlayer.gravityScale = 1.5f;
    }
    // utile pour les plafond cours.
    private void OnCollisionExit2D(Collision2D colli)
    {        
        rbPlayer.gravityScale = 1.5f;
        timeOnCeilling = 0;

        
    }
    void CeillingSlide()
    {
    //    if (isOnCeilling)
    //    {

    //        if (timeOnCeilling > 0)
    //        {
    //            rb.gravityScale = 0;
    //            timeOnCeilling -= Time.deltaTime;
                
    //        }

    //        else if (timeOnCeilling <= 0)
    //        {
    //            rb.gravityScale = 1.5f;
    //            timeOnCeilling = 0;
    //        }
                
    //    }

       
    //    else
    //    { 
    //        rb.gravityScale = 1.5f;
        
    //    }
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (canTakeFallDamage)
        {
            // retir des pv en fct de la velocity relative * les degat que tu prend relative
            // a 2 sec de chute.
        }

    }
    
    public void CoroutineStarter()
    {
        if (cS.dischargeType == CircleScript.DischargeType.DisRight)
        {
            crD = StartCoroutine(NoDragUntilTouchingGround());
        }
    }
    IEnumerator NoDragUntilTouchingGround()
    {
        while(IsGroundedOrInOnCircle() == false)
        {
            rbPlayer.drag = 0;
            yield return null;
        }
        if (IsGroundedOrInOnCircle())
        {
            yield return new WaitForSeconds(0.5f);
            ApplyLinearDrag();
        }
        crD = null;
    }
}
