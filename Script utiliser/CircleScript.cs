using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleScript : MonoBehaviour
{

    public float dureeDisparition = 3;
    bool absorbe = false;
    public float energyAbsorbed = 0;
    Rigidbody2D rb;
    public PlayerMovement player;
    
    public bool InputR => Input.GetKey(KeyCode.R);
    public bool InputZ => Input.GetKeyDown(KeyCode.Z);
    public bool InputLeft => Input.GetKey(KeyCode.LeftArrow);
    public bool InputRight => Input.GetKey(KeyCode.RightArrow);
    public bool InputUp => Input.GetKey(KeyCode.UpArrow);
    public bool InputDown => Input.GetKey(KeyCode.DownArrow);
    public bool dontMove = false;
    public DischargeType dischargeType;
    public enum DischargeType
    {
        None,
        DisNeutral, //L'energie explose en cercle, donnant du momentum egal a toute les direction
        DisDown, //Tout en down, rien ailleur, même principe poure les autres.
        DisUp,
        DisLeft,
        DisRight
    }

    void Start()
    {
        StartCoroutine(DisappearTime());
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<PlayerMovement>();
        
    }


    void Update()
    {
        SetAbsorptionMode();
        InputDischarge();
        RemoveEnergy();
    }
    void FixedUpdate()
    {
        DontMoveOnBlackDisk();
        DischargeAction(); // a revoir car ça ne fait pas ce que je veux.

    }
    void LateUpdate()
    {
        
    } 

    private void InputDischarge() // a remplire
    {
        if (InputZ && InputUp)
        {
            dischargeType = DischargeType.DisUp;
            Debug.Log("DisUp");
        }
        else if (InputZ && InputDown)
        {
            dischargeType= DischargeType.DisDown;
            Debug.Log("DisD");
        }
        else if(InputZ && InputRight)
        {
            dischargeType = DischargeType.DisRight;
            Debug.Log("DisR");
        }
        else if(InputZ && InputLeft)
        {
            dischargeType = DischargeType.DisLeft;
            Debug.Log("DisL");
        }

        else if(InputZ)
        {
            dischargeType = DischargeType.DisNeutral;
            Debug.Log("DisA");
            //DischargeAround
        }
        
    }
    public void DischargeAction()
    {
        if (dischargeType == DischargeType.DisUp)
        {
            DischargeMethode(0, 1);
            Debug.Log("DisUpFait");
            dischargeType = DischargeType.None;
        } 
        else if(dischargeType == DischargeType.DisDown)
        {
            DischargeMethode(0,-1);
            Debug.Log("DisDownFait");
            dischargeType = DischargeType.None;
        }
        else if(dischargeType == DischargeType.DisRight)
        {
            player.freeMomentum = true;
            
            DischargeMethode(1,0.1f);
            Debug.Log("DisRFait");
            dischargeType = DischargeType.None;
        }
        else if(dischargeType == DischargeType.DisLeft)
        {
            DischargeMethode(-1,0.1f);
            Debug.Log("DisLFait");
            dischargeType = DischargeType.None;
        }
    }
   
    void DischargeMethode(float directionX,float directionY)
    {
        player.rbPlayer.drag = 0;
        player.rbPlayer.AddForce(new Vector2(directionX *player.energyAbsorbed, directionY *player.energyAbsorbed),ForceMode2D.Impulse);
        player.DischargeEnergy();
       
    }

    private void DontMoveOnBlackDisk()
    {
        if (dontMove)
        {
            player.rbPlayer.velocity = Vector2.zero;
        }
        
    }


    void RemoveEnergy()
    {
        if(InputLeft)
        {
            energyAbsorbed = 0;
        }
    }

    public void SetAbsorptionMode()
    {
        if(InputR)
        {
            absorbe = true;
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
            GetComponent<SpriteRenderer>().color = Color.black;
            
        }
        else
        {
            absorbe = false;
            GetComponent<SpriteRenderer>().color = Color.white;
            if(rb != null)
                rb.isKinematic = false;
        }
        
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Identifier ce qui a été toucher
        Debug.Log("le disque a toucher " + collision.gameObject.name);

        var getRb = collision.gameObject.GetComponent<Rigidbody2D>();


        if (absorbe)
        {
            player.StockEnergy(collision.relativeVelocity.magnitude,collision.rigidbody.mass);
            // faire un slider bar pour l'energie et la capper. Dans splayer*
            // crée par la suite une fonction qui decharge (Applique une force)

        }
    }
    private void OnCollisionStay2D(Collision2D colS) // travailler ça en urgence.
    {
        var getRb = colS.gameObject.GetComponent<Rigidbody2D>();
        if(absorbe)
        {
            dontMove = true;
            if(player.Space)
            {
                dontMove = false;
            }
            
        }
        else
        {
            dontMove = false;
        }
    }
    

    IEnumerator DisappearTime()
    {
        yield return new WaitForSeconds(dureeDisparition);
        Destroy(gameObject);
    }

   
    
}
