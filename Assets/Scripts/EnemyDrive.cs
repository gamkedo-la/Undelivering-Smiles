using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CarDrive))] //tells script it requires CarDrive
public class EnemyDrive : MonoBehaviour
{

    //remove below once you learn how to reference the CameraSwitch script
    public Camera mainCamera;
    public Camera enemyCamera;

    private CarDrive carDrive;

    // ^ going to write some better variables
    public GameObject wayPointSpawnedAt;
    public Transform nextWayPoint;

    public GameObject pfxBurst;

    //private Transform currentWayPoint = wayPoint; ask Chris why this throws an error
    private Transform currentWayPoint;

    private bool cinderBlock = false;
    private bool camSwitch = false;
    private float gasControl = 0.0f;
    private float turnControl = 0.0f;
    private float turnAmt = 0.0f;

    //REMOVE BELOW HELPER FUNCTIONS ONCE CAMSWITCH SCRIPT WORKS
    // Call this function to disable main camera,
    // and enable enemy camera.
    public void ShowEnemyView() {
        mainCamera.enabled = false;
        enemyCamera.enabled = true;
    }    
    // Call this function to enable main camera,
    // and disable enemy camera.
    public void ShowMainView() {
        mainCamera.enabled = true;
        enemyCamera.enabled = false;
    }

    private void hitVan(Vector3 hitDirectionAndForce, Vector3 spinDirectionAndForce, float upForce){ //transform.r,l,back,f
        //Debug.Log("hit van called");
        carDrive.rb.drag = 0.0f;
        carDrive.rb.angularDrag = 0.0f;
        carDrive.rb.freezeRotation = false;
        carDrive.rb.AddForce(hitDirectionAndForce + (Vector3.up * upForce));
        carDrive.rb.angularVelocity = spinDirectionAndForce; //in radians
        this.enabled = false; //disables script - remember this is connected to a return-kill block at the top of the collision code, it's a two parter
        Destroy(gameObject, 3.0f);
    }

    void OnCollisionEnter(Collision coll){
        if(this.enabled == false){
            return;
        }
        /*
        if(hasCollided == true){
            return;
        }
        */
        if(LayerMask.LayerToName(coll.gameObject.layer) == "Player"){
            
            GameObject.Instantiate(pfxBurst, transform.position, Quaternion.identity, transform);
            // Debug.Log("I've been bumped! " + gameObject.name + " by " + coll.gameObject.name);
            //Destroy(gameObject);
            Vector3 relativeHitPt = transform.InverseTransformPoint(coll.contacts[0].point); //makes it relative to the point hit
            float angle = Mathf.Atan2(relativeHitPt.x, relativeHitPt.z) * Mathf.Rad2Deg;
            //Debug.Log(coll.collider.gameObject.name + " " + angle);
            Rigidbody rb = coll.collider.gameObject.GetComponentInParent<Rigidbody>();
            Vector3 colliderVelocity = rb.velocity * 2.0f;
            //note about tuning ranges: think about placing the van on a clock, the more "hours" a side occupies, the bigger it's range
            //this is why (at the time of oo) front/back is 20.0 and the sides are 160.0, adds up to 180, but the sides are way bigger

            //TODO: implement a collision cool down so you aren't double blasting the enemy van with force
            //when you implement addForce or addForcePosition, use a BIG multiplier, the physics units are tiny and might not show visible change
            //even if they are working
            float frontBackAngRange = 40.0f; 

            if(Mathf.Abs(angle) < frontBackAngRange/2){
                //Debug.Log("hit from front");
                hitVan(transform.forward * -500.0f + colliderVelocity, transform.right * 3000.0f, 500.0f);
            } else if (angle > 0.0f && angle < 180.0f - frontBackAngRange/2) {
                //Debug.Log("hit from right");
                hitVan(transform.right * -500.0f + colliderVelocity, transform.forward * 3000.0f, 500.0f);
            } else if (angle < 0.0f && angle > -180.0f + frontBackAngRange/2) {
                //Debug.Log("hit from left");
                hitVan(transform.right * 500.0f + colliderVelocity, transform.forward * -6000.0f, 500.0f);
            } else {
                //Debug.Log("hit from back");
                hitVan(transform.forward * 500.0f + colliderVelocity, transform.right * -3000.0f, 500.0f);
            }
            //hasCollided = true;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        carDrive = gameObject.GetComponent<CarDrive>();
        //cameraSwitch = gameObject.GetComponent<CameraSwitch>();
        carDrive.BaseStart();
        //currentWayPoint = wayPoint;
        cinderBlock = true;
        if(wayPointSpawnedAt){
            nextWayPoint = wayPointSpawnedAt.GetComponent<WaypointData>().next.transform;
        }
    }

    protected float AngleAroundAxis (Vector3 dirA, Vector3 dirB, Vector3 axis) {
		dirA = dirA - Vector3.Project(dirA, axis);
		dirB = dirB - Vector3.Project(dirB, axis);
		float angle = Vector3.Angle(dirA, dirB);
		return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
	}

    void SteerTowardPoint() {
        if(nextWayPoint){
            float distTo = Vector3.Distance(transform.position, nextWayPoint.position);
            float closeEnoughToWaypoint = 10.0f;
            if(distTo < closeEnoughToWaypoint) {
                //currentWayPoint = wayPoint2; change to next, current doesn't really make sense
                nextWayPoint = nextWayPoint.GetComponent<WaypointData>().next.transform;
            }

            turnAmt = AngleAroundAxis(transform.forward, nextWayPoint.position - transform.position, Vector3.up);
            if(Mathf.Abs(turnAmt) > 100.0f){ 
                Debug.LogWarning("waypoint is behind van");
                nextWayPoint = nextWayPoint.GetComponent<WaypointData>().next.transform;
                turnAmt = AngleAroundAxis(transform.forward, nextWayPoint.position - transform.position, Vector3.up);
            } //if waypoint happens to be behind the car

            float angDeltaForGentleTurn = 200.0f;
            float angDeltaForSharpTurn = 200.0f;
            float gentleTurn = 0.5f;
            float sharpTurn = 1.0f;
            float gentleTurnEnginePower = 0.9f;
            float sharpTurnEnginePower = 0.6f;
            int pathWay = 0;

            if(turnAmt < -angDeltaForSharpTurn) {
                pathWay = -1;
                turnControl = -sharpTurn;
                gasControl = sharpTurnEnginePower;
            } else if(turnAmt > angDeltaForSharpTurn) {
                pathWay = 1;
                turnControl = sharpTurn;
                gasControl = sharpTurnEnginePower;
            } else if(turnAmt < -angDeltaForGentleTurn) {
                pathWay = -2;
                turnControl = -gentleTurn;
                gasControl = gentleTurnEnginePower;
            } else if(turnAmt > angDeltaForGentleTurn) {
                pathWay = 2;
                turnControl = gentleTurn;
                gasControl = gentleTurnEnginePower;
            } else {
                pathWay = 0;
                turnControl = 0.0f;
                gasControl = 1.0f;
            }

            //Debug.Log(pathWay + ", " + turnAmt + ", " + nextWayPoint.name);
        }
	}
	

    // Update is called once per frame
    void Update()
    {

        if(Input.GetKeyUp(KeyCode.J))
        {
            //hitVan(transform.right * 500.0f, transform.forward * -3000.0f);
        }

        if(Input.GetKeyUp(KeyCode.L))
        {
            //hitVan();
        }
        if(Input.GetKeyUp(KeyCode.I))
        {
            //hitVan();
        }
        if(Input.GetKeyUp(KeyCode.K))
        {
            //Debug.Log("hit from k");
        }

        if(Input.GetKeyUp(KeyCode.Space)){
            cinderBlock = !cinderBlock;
        }  

        /*
        if(Input.GetKeyUp(KeyCode.C)){
            //camSwitch = !camSwitch;
        }  

        if(camSwitch){
            ShowEnemyView();
        }

        else if(!camSwitch){
            ShowMainView();
        }
        */


        //Debug.Log(cameraSwitch);

        /*
        if(cinderBlock){
            cameraSwitch.ShowEnemyView();
        }

        else if(!cinderBlock){
            cameraSwitch.ShowMainView();
        }
        */
    }

    void FixedUpdate(){

        pfxBurst.transform.position = transform.position;
        //Debug.Log(pfxBurst.transform.position);
        //Debug.Log(transform.position);

        Vector3 flatForward = transform.forward;
        flatForward.y = 0.0f;   

        /*
        float distTo = Vector3.Distance(transform.position, nextWayPoint.position);
		float closeEnoughToWaypoint = 10.0f;
		if(distTo < closeEnoughToWaypoint) {
            //currentWayPoint = wayPoint2; change to next, current doesn't really make sense
            nextWayPoint = nextWayPoint.GetComponent<WaypointData>().next.transform;
        }

        float turnAmt = AngleAroundAxis(transform.forward, nextWayPoint.position - transform.position, Vector3.up);
        */

        carDrive.BaseUpdate(cinderBlock);
        SteerTowardPoint();

        //call steerToPoint here: will continuously steer and apply the right gas and turn to the car per time
        //below the private, class level variables gasControl and turnControl will be applied to the actual physics calculations 

        if(cinderBlock)
        {
            //add to the current velocity according while accelerating
            //carDrive.currentVelocity = carDrive.currentVelocity + (carDrive.accelerationRate * Time.deltaTime);
            //TODO throw drivepower in here? like...
            carDrive.currentVelocity = carDrive.currentVelocity + (carDrive.accelerationRate * gasControl * Time.deltaTime);
        }
        //else if(Input.GetAxisRaw("Vertical") < 0.0f)
        else
        {
            //subtract from the current velocity while decelerating
            carDrive.currentVelocity = carDrive.currentVelocity - (carDrive.decelerationRate * Time.deltaTime);
        }   

        //ensure the velocity never goes out of the initial/final boundaries
        carDrive.currentVelocity = Mathf.Clamp(carDrive.currentVelocity, carDrive.initialVelocity, carDrive.finalVelocity);  

        //Debug.Log("script being reached" + cinderBlock);

        if(cinderBlock == true) //forward
        {
            //Debug.Log(carDrive.rb.velocity);
            carDrive.rb.velocity = carDrive.rb.velocity*carDrive.driftPercent + (1.0f - carDrive.driftPercent) * flatForward * carDrive.currentVelocity; //vel ALREADY takes place over time
            transform.Rotate(Vector3.up, (turnAmt * 4.0f) * Time.deltaTime);
            //transform.Rotate(Vector3.up, (turnControl*1000.0f) * Time.deltaTime);
            
        }
        else if((cinderBlock == false)) //brake
        {
            if(carDrive == null){
                Debug.Log("carDrive missing");
            } 
            else if(carDrive.rb == null){
                Debug.Log("rb is missing");
            }
            
            carDrive.rb.velocity = carDrive.rb.velocity * carDrive.brakeDecayPercent;
            
        }
        // if above conditions aren't met, the vehicle will decelerate

    }
}