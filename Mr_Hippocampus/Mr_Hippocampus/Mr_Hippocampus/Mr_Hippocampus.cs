using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

/*
Mr. Hippo enters three different modes:

COLLECT:
When Mr. Hippocampus has less than the majority (5 targets) needed to win,
it will collect the nearest targets until it does. It will return to the base
when it has at least three targets when there are no targets at the base. Once there are
at least three targets at the base, it will collect and return at least the minimum remaining balls 
(5 - # balls at base) to get the majority. This is to reduce the number of trips it needs to make.

DEFENSE:
When Mr. Hippocampus has at least 5 targets in its base, it will stay there and
laser the enemy if it approaches. If it is able, it will avoid enemy lasers by
moving whenever the enemy is directly facing it. Otherwise, it will shoot a constant
defensive line of lasers around the base. 

CRIME:
If an enemy is carrying at least one target nearby, Mr. Hippocampus will shoot it. 
If the enemy has the majority of balls and there are no more balls on the floor, 
Mr. Hippocampus will steal from the enemy. 

Mr. Hippocampus will try to dodge if it can; it has punishments for being frozen and dropping
targets.

*/
public class Mr_Hippocampus : CogsAgent
{
    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
    
    // Initialize values
    protected override void Start()
    {
        base.Start();
        AssignBasicRewards();
    }

    // For actual actions in the environment (e.g. movement, shoot laser)
    // that is done continuously
    protected override void FixedUpdate() {
        base.FixedUpdate();
        
        LaserControl();
        // Movement based on DirToGo and RotateDir
        moveAgent(dirToGo, rotateDir);


        
    }



    
    // --------------------AGENT FUNCTIONS-------------------------

    // Get relevant information from the environment to effectively learn behavior
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent velocity in x and z axis 
        var localVelocity = transform.InverseTransformDirection(rBody.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);

        // Time remaning
        sensor.AddObservation(timer.GetComponent<Timer>().GetTimeRemaning());  

        // Agent's current rotation
        var localRotation = transform.rotation;
        sensor.AddObservation(transform.rotation.y);

        // Agent and home base's position
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(baseLocation.localPosition);

        // for each target in the environment, add: its position, whether it is being carried,
        // and whether it is in a base
        foreach (GameObject target in targets){
            sensor.AddObservation(target.transform.localPosition);
            sensor.AddObservation(target.GetComponent<Target>().GetCarried());
            sensor.AddObservation(target.GetComponent<Target>().GetInBase());
        }
        
        // Whether the agent is frozen
        sensor.AddObservation(IsFrozen());

        // How many balls are in our base
        sensor.AddObservation(myBase.GetComponent<HomeBase>().GetCaptured());

        

        
    }

    

    // For manual override of controls. This function will use keyboard presses to simulate output from your NN 
    public override void Heuristic(float[] actionsOut)
    {
        var discreteActionsOut = actionsOut;
        discreteActionsOut[0] = 0; //Simulated NN output 0   // whether or not u r moving forward or backward
        discreteActionsOut[1] = 0; //....................1   // whether or not u r moving left or right
        discreteActionsOut[2] = 0; //....................2
        discreteActionsOut[3] = 0; //....................3

        //TODO-2: Uncomment this next line when implementing GoBackToBase();
        discreteActionsOut[4] = 0; 


       
        if (Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }       
        if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            //TODO-1: Using the above as examples, set the action out for the left arrow press
            discreteActionsOut[1] = 2;
            
        }

        // Rotate towards enemy
        if (Input.GetKey(KeyCode.R)){
            discreteActionsOut[1] = 3;
        }


        //Shoot
        if (Input.GetKey(KeyCode.Space)){
            discreteActionsOut[2] = 1;
        }

        //GoToNearestTarget
        if (Input.GetKey(KeyCode.A)){
            discreteActionsOut[3] = 1;
        }


        //TODO-2: implement a keypress (your choice of key) for the output for GoBackToBase();

        if (Input.GetKey(KeyCode.B)){
            discreteActionsOut[4] = 1;
        }

        // if enemy is carrying at least 1 ball and they are close by, rotate towards them so we can shoot them
        if (GetEnemyCarrying() >= 1 && DistanceToEnemy() <= 5){
            discreteActionsOut[1] = 3;
        }

        // if enemy has majority of the balls, get more balls
        if (GetEnemyCaptured() > myBase.GetComponent<HomeBase>().GetCaptured()){
            discreteActionsOut[3] = 1;
        }
    }
          
            



     // What to do when an action is received (i.e. when the Brain gives the agent information about possible actions)
    public override void OnActionReceived(float[] act)
    {
        int forwardAxis = (int)act[0]; //NN output 0  // forward or back

        //TODO-1: Set these variables to their appopriate item from the act list
        int rotateAxis = (int)act[1]; // left or right
        int shootAxis = (int)act[2]; 
        int goToTargetAxis = (int)act[3];
        int goToBaseAxis = (int)act[4]; // go to base

        
        MovePlayer(forwardAxis, rotateAxis, shootAxis, goToTargetAxis, goToBaseAxis); 
    }


// ----------------------ONTRIGGER AND ONCOLLISION FUNCTIONS------------------------
    // Called when object collides with or trigger (similar to collide but without physics) other objects 
    protected override void OnTriggerEnter(Collider collision) // e.g., base
    {
        

        
        if (collision.gameObject.CompareTag("HomeBase") && collision.gameObject.GetComponent<HomeBase>().team == GetTeam())
        {
            // when we have 5 balls or more, just stay here and DEFEND
            if (myBase.GetComponent<HomeBase>().GetCaptured() >= 5){
                AddReward(4.0f);
            } 

            // collect balls if we have less than 5 balls
            if (myBase.GetComponent<HomeBase>().GetCaptured() < 5){

                // collect at least 3 balls before coming back if there are no balls at base
                if (GetCarrying() >= 3 && myBase.GetComponent<HomeBase>().GetCaptured() == 0){
                    AddReward(2.0f);
                } 

                // but if there is least 3 balls at base and less than 5, collect at the least the minimum balls needed to reach majority
                if (GetCarrying() >= GetMinimumBalls() && myBase.GetComponent<HomeBase>().GetCaptured() >= 3){
                    AddReward(GetMinimumBalls() * 0.5f);
                } 

            }

            

        }
        base.OnTriggerEnter(collision);
    }

    protected override void OnCollisionEnter(Collision collision) 
    {
        

        //target is not in my base and is not being carried and I am not frozen
        if (collision.gameObject.CompareTag("Target") && collision.gameObject.GetComponent<Target>().GetInBase() != GetTeam() && collision.gameObject.GetComponent<Target>().GetCarried() == 0 && !IsFrozen())
        {

            SetReward(1.0f);

        }

        if (collision.gameObject.CompareTag("Wall"))
        {

            AddReward(-0.1f);
            
        }
        base.OnCollisionEnter(collision);
    }





    //  --------------------------HELPERS---------------------------- 
     private void AssignBasicRewards() {
        rewardDict = new Dictionary<string, float>();

        rewardDict.Add("frozen", -0.1f);
        rewardDict.Add("shooting-laser", 0f); 
        rewardDict.Add("hit-enemy", 0.1f);
        rewardDict.Add("dropped-one-target", -0.1f); 
        rewardDict.Add("dropped-targets", -0.2f);
    }
    
    private void MovePlayer(int forwardAxis, int rotateAxis, int shootAxis, int goToTargetAxis, int goToBaseAxis)  //int goToEnemyAxis)
    //TODO-2: Add goToBase as an argument to this function ^
    {
        dirToGo = Vector3.zero;
        rotateDir = Vector3.zero;

        Vector3 forward = transform.forward;
        Vector3 backward = -transform.forward;
        Vector3 right = transform.up;
        Vector3 left = -transform.up;

        //fowardAxis: 
            // 0 -> do nothing
            // 1 -> go forward
            // 2 -> go backward
        if (forwardAxis == 0){
            //do nothing. This case is not necessary to include, it's only here to explicitly show what happens in case 0
        }
        else if (forwardAxis == 1){
            dirToGo = forward;
        }
        else if (forwardAxis == 2){
            //TODO-1: Tell your agent to go backward!

            dirToGo = backward;
            
        }

        //rotateAxis: 
            // 0 -> nothing
            // 1 -> go right
            // 2 -> go left
            // 3 -> rotate towards the enemy
        if (rotateAxis == 0){
            //do nothing
            
        }
        
        //TODO-1 : Implement the other cases for rotateDir

        else if (rotateAxis == 1){ 
            rotateDir = right;
        }

        else if (rotateAxis == 2){ 
            rotateDir = left;
        }

        // rotate towards the enemy
        else if (rotateAxis == 3){ 
            RotateTowardsEnemy();
        }

        //shoot
        if (shootAxis == 1){
            SetLaser(true);
        }
        else {
            SetLaser(false);
        }

        //go to the nearest target
        
        if (goToTargetAxis == 1){
            GoToNearestTarget();
            }
        

        //TODO-2: Implement the case for goToBaseAxis

        if (goToBaseAxis == 1){
            GoToBase(); 
        }





    }

    // Go to home base
    private void GoToBase(){
        TurnAndGo(GetYAngle(myBase));
    }

    // Go to the nearest target
    private void GoToNearestTarget(){
        GameObject target = GetNearestTarget(); //change
        if (target != null){
            float rotation = GetYAngle(target);
            TurnAndGo(rotation);
        }        
    }

    // Rotate towards the enemy 
    private void RotateTowardsEnemy(){
        float rotation = GetYAngle(enemy);
        TurnOnly(rotation);      
    }

    // Rotate and go in specified direction
    private void TurnAndGo(float rotation){

        if (rotation < -5f){
            rotateDir = transform.up;
        }
        else if (rotation > 5f){
            rotateDir = -transform.up;
        }
        else {
            dirToGo = transform.forward;
        }
    }

    // rotate in specified direction but don't go forwards
    private void TurnOnly(float rotation){

        if (rotation < -5f){
            rotateDir = transform.up; //right
        }
        else if (rotation > 5f){
            rotateDir = -transform.up; //left
        }
        
    }


    // return reference to nearest target
    protected GameObject GetNearestTarget(){
        float distance = 200;
        GameObject nearestTarget = null;
        foreach (var target in targets)
        {
            float currentDistance = Vector3.Distance(target.transform.localPosition, transform.localPosition);
            if (currentDistance < distance && target.GetComponent<Target>().GetCarried() == 0 && target.GetComponent<Target>().GetInBase() != team){
                distance = currentDistance;
                nearestTarget = target;
            }
        }
        return nearestTarget;
    }

    private float GetYAngle(GameObject target) {
        
       Vector3 targetDir = target.transform.position - transform.position;
       Vector3 forward = transform.forward;

      float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);
      return angle; 
        
    }

    // get distance between us and the enemy
    private float DistanceToEnemy(){
        return Vector3.Distance(transform.localPosition, enemy.transform.localPosition);
    }

    
    // returns how many balls the enemy is carrying
    private int GetEnemyCarrying(){
        int enemyCarrying = 0;
        foreach (GameObject target in targets){

            int carrying = target.GetComponent<Target>().GetCarried();

            // if it's not being carried by no one and not being carried by us, 
            // add 1 to the number of targets being carried by the enemy

            if (carrying != 0 && carrying != team){
                enemyCarrying += 1;
            }
        }

        return enemyCarrying;
    }

    // returns how many balls the enemy has in their base
    private int GetEnemyCaptured(){
        int enemyCaptured = 0;
        foreach (GameObject target in targets){

            int captured = target.GetComponent<Target>().GetInBase();

            // if it's not been captured by no one and not been captured by us, 
            // add 1 to the number of targets being carried by the enemy

            if (captured != 0 && captured != team){
            enemyCaptured += 1;
            }
        }

        return enemyCaptured;
    }
    
    // calculates the minimum number of balls that we need to collect in order to have majority of balls
    private int GetMinimumBalls(){
        int majority = 5;

        int minimumNeeded = majority - myBase.GetComponent<HomeBase>().GetCaptured();
        return minimumNeeded;
    }

}
