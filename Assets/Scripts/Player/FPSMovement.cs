using UnityEngine;

public class FPSMovement : MonoBehaviour
{
    Vector3 playerVelocity;
    Vector2 mouseRotation;
    Vector2 keyboardInput;
    CharacterController characterController;
    bool isJumping;
    float airGlideTimer;
    bool onSlope;
    float slopeAngle;
    Vector3 slideDirection;
    bool altSpeedFlag;
    bool movementIsLocked;
    bool mouseIsLocked;
    float speedCap;
    
    /* Set in Editor */
    public Transform cameraHolder;
    public float headHeight = 4f;
    public float gravity = 10f;
    public float runSpeed = 15;
    public float walkSpeed = 6f;
    public float runAcceleration = 10f;
    public float runDeacceleration = 10f;
    public bool setDefaultToWalkSpeed;
    public float friction = 5f;
    public float verticalLeap = 10f;
    public float slopeStepCheckLenght = 3f;
    public float slopeStepPullForce = 100f;
    /* Set in Editor END */

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        mouseRotation = new Vector2(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y);
        keyboardInput = Vector2.zero;
        airGlideTimer = 0;

        speedCap = setDefaultToWalkSpeed ? walkSpeed : runSpeed;              
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSurfaceInfo();
        
        if(!mouseIsLocked)
            MouseInput();

        if(!movementIsLocked)
        {
            AltSpeed(); // Toggle walking
            KeyboardInput();
        
            if(characterController.isGrounded)
            {   
                isJumping = false;
                Friction();
                if(slopeAngle > characterController.slopeLimit)
                    Slide();
                else        
                    GroundMove();
            }
            else
            {
                AirMove();
                if(onSlope && !isJumping)
                    playerVelocity.y -= characterController.height * 0.5f * slopeAngle * gravity * Time.deltaTime; // Pull down on slope
            }

            if(Input.GetButton("Jump") && characterController.isGrounded && slopeAngle < characterController.slopeLimit)
            {
                isJumping = true;
                playerVelocity.y = verticalLeap;
            }

            characterController.Move(playerVelocity * Time.deltaTime);

            MoveCamera();
        }
    }

    void LateUpdate()
    {
        MoveCamera();
    }

    void AltSpeed()
    {
        if(Input.GetButtonDown("Toggle Walk"))
            if(speedCap == walkSpeed)
                speedCap = runSpeed;
            else
                speedCap = walkSpeed;
    }

    void Slide()
    {
        Vector3 wishDirection = new Vector3(slideDirection.x, 0, slideDirection.z);
        //wishDirection = transform.TransformDirection(wishDirection);
        wishDirection.Normalize();

        Accelerate(wishDirection, speedCap, runAcceleration);
    }

    void UpdateSurfaceInfo()
    {
        RaycastHit hit;

        if(Physics.Raycast(transform.position, Vector3.down, out hit, (characterController.height * 0.5f) + slopeStepCheckLenght) && !isJumping)
        {
            slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            onSlope = slopeAngle != 0 ? true : false;
            
            Vector3 hitNormal = hit.normal;
            slideDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
            Vector3.OrthoNormalize(ref hitNormal, ref slideDirection);
        }
        else
            onSlope = false;
    }

    void GroundMove()
    {
        Vector3 wishDirection = new Vector3(keyboardInput.x, 0, keyboardInput.y);
        wishDirection = transform.TransformDirection(wishDirection);
        wishDirection.Normalize();

        Accelerate(wishDirection, speedCap, runAcceleration);

        if(!onSlope || characterController.velocity.magnitude == 0)
            playerVelocity.y = -gravity * Time.deltaTime; // Reset gravitiy
        
        airGlideTimer = 0f;
    }

    void Accelerate(Vector3 direction, float speed, float acceleration)
    {
        float currentspeed = Vector3.Dot(playerVelocity, direction);
        float addspeed = speed - currentspeed;
        
        if(addspeed <= 0)
            return;
        float accelspeed = acceleration * Time.deltaTime * speed;
        if(accelspeed > addspeed)
            accelspeed = addspeed;

        //Vector2 velocityXZ = new Vector2(playerVelocity.x, playerVelocity.z);

        // if(velocityXZ.magnitude < speedCap + 3f)
        // {
            playerVelocity.x += accelspeed * direction.x;
            playerVelocity.z += accelspeed * direction.z;
        //}
    }

    void Friction()
    {
        float currentSpeed = new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude;
        float control = currentSpeed < runDeacceleration ? runDeacceleration : currentSpeed;
        float drop = control * friction * Time.deltaTime;
        float newspeed = currentSpeed - drop;
        
        if(newspeed < 0)
            newspeed = 0;
        if(currentSpeed > 0)
            newspeed /= currentSpeed;

        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }

    void AirMove()
    {
        Vector3 wishDirection = new Vector3(keyboardInput.x, 0, keyboardInput.y);
        wishDirection = transform.TransformDirection(wishDirection);
        wishDirection.Normalize();

        Accelerate(wishDirection, speedCap, runAcceleration * 0.1f);

        float currentSpeed = new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude;

        if(!isJumping && airGlideTimer < currentSpeed * 0.005f) // Glide slightly when running off edge
        {
            playerVelocity.y = 0f;
        }
        else if((isJumping || airGlideTimer > currentSpeed * 0.005f) && playerVelocity.y < 30f)
            playerVelocity.y -= gravity * Time.deltaTime;
        
        airGlideTimer += Time.deltaTime;
    }

    void KeyboardInput()
    {
        keyboardInput.x = Input.GetAxisRaw("Horizontal");
        keyboardInput.y = Input.GetAxisRaw("Vertical"); // Z axis in 3D space
    }

    void MouseInput()
    {
        mouseRotation.x -= Input.GetAxis("Mouse Y");
        mouseRotation.y += Input.GetAxis("Mouse X");

        // Clamp the X rotation
        if(mouseRotation.x < -90)
            mouseRotation.x = -90;
        else if(mouseRotation.x > 90)
            mouseRotation.x = 90;
        
        transform.rotation = Quaternion.Euler(0, mouseRotation.y, 0); // Rotates the collider        
        cameraHolder.transform.rotation = Quaternion.Euler(mouseRotation.x, mouseRotation.y, 0); // Rotates the camera 
    }

    void MoveCamera()
    {
        cameraHolder.transform.position = this.transform.position;
        cameraHolder.transform.position = new Vector3(cameraHolder.transform.position.x, this.transform.position.y + headHeight - (characterController.height * 0.5f), cameraHolder.transform.position.z);
    }

    public void SetMovementLock(bool state)
    {
        movementIsLocked = state;
    }

    public void SetMouseLookLock(bool state)
    {
        mouseIsLocked = state;
    }

    public float GetVelocity()
    {
        Vector3 velocity = characterController.velocity;
        velocity.y = 0f;
        return velocity.magnitude;
    }

    public bool IsGrounded()
    {
        return characterController.isGrounded;
    }

    private void OnGUI()
    {
        //GUI.Label(new Rect(0, 0, 400, 100), "FPS: " + fps, style);
        var ups = characterController.velocity;
        ups.y = 0;
        GUI.Label(new Rect(300, 15, 400, 100), "Speed: " + Mathf.Round(ups.magnitude * 100) / 100 + "ups");
    }
}
