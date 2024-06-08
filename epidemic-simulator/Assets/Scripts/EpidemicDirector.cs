using System.Collections.Generic;
using UnityEngine;

public class EpidemicDirector : MonoBehaviour
{
    const float CONSTANT = (3.61259412f * 0.0000000000000000001f);

    [Header("Simulation Start")]
    [SerializeField] private int residentCount;

    [Header("Resident Data")]
    [SerializeField] private List<Resident> residents = new List<Resident>();
    [Header("Buildings")]
    [SerializeField] private Transform[] buildings;
    public enum BuildingType{
        OFFICE,
        SCHOOL,
        RECREATION,
        HOUSE
    }
    [SerializeField] private BuildingType[] buildingTypes;

    private void Start()
    {
        for (int i = 0; i < residentCount; i++)
        {
            int age = AgeDistribution(Random.Range(1f, 100f));
            Resident res = new Resident(age);
            residents.Add(res);
            // PrintSchedule(res);
        }

        for (int i = 0; i < buildingTypes.Length; i++){
            
        }
    }

    void PrintSchedule(Resident resident)
    {
        string output = "";
        for (int i = 0; i < 7; i++)
        {
            output += "[";
            for (int j = 0; j < 24; j++)
            {
                output += resident.schedule.weeklyRoutine[i,j].ToString().Substring(0,2) + (j != 23 ? ", " : "");
            }
            output += "]\n";
        }
        print(output);
    }

    public static int AgeDistribution(float x)
    {   
        float y = -0.349402f + Mathf.Pow(CONSTANT, 0.465665f * x) + 0.778238f * x;
        return (int)y;
    }
}

[System.Serializable]
public class Resident
{
    public static string nameChunk = "asteriosalyssamaximilianbrunhildelisadomitillaseppstyopasakchainataliyaprasadfarrahendricus";
    public static string leadCharacter = "AAAASDFJHGSHKFHFDGHSGLKJFHGSDGGHAFDJLKGHALSFWWTTTTTTTPPPPPMMMMNNBBCCCCZYXQUPIURYTQWERTYUIOPLKJHGFDSAZXCVBNM";

    public enum Role
    {
        WORKER,
        STUDENT,
        FREERIDER
    }

    public string name;
    public Role role;
    public int age;
    public float immuneStrength;
    public Schedule schedule;

    //locations
    public int house;
    public int schoolOrWork;

    public Resident(int age)
    {
        this.age = age;

        CreateSleepSchedule();
        GenerateName();
        FindRole();

        float ageBased = Mathf.Sin((age + 10f) * Mathf.PI / 100f) * 0.1f;
        immuneStrength = Random.Range(0.1f, 0.6f) + (schedule.sleepingHours / 11f) * 0.3f + ageBased; //immune strength calculation based on randomness + how much they sleep + age

        GenerateWeeklyRoutine();
    }

    void CreateSleepSchedule()
    {
        int sleepStart = Random.Range(21, 24);
        int sleepEnd = Random.Range(5, 8);
        int sleepingHours = sleepEnd + (24 - sleepStart);
        schedule = new Schedule(new Schedule.Action[7, 24], sleepStart, sleepEnd, sleepingHours);
    }

    void GenerateWeeklyRoutine()
    {
        Schedule.Action[,] schedule = this.schedule.weeklyRoutine;
        SetActionBlock(schedule, 5, 5, 14, 14, 0, 7, Schedule.Action.HOME, true);

        if(role != Role.FREERIDER){
            SetActionBlock(schedule, 16, 18, 2, 6, 0, 5, Schedule.Action.FREETIME, true);
            SetActionBlock(schedule, 6, 16, 4, 8, 5, 7, Schedule.Action.FREETIME, true);
        }else{
            SetActionBlock(schedule, 6, 11, 4, 12, 0, 7, Schedule.Action.FREETIME, true);
        }

        //set errands
        if(role == Role.WORKER) {
            SetActionBlock(schedule, 17, 17, 1, 5, 0, 5, Schedule.Action.ERRANDS, true);
            SetActionBlock(schedule, 8, 16, 1, 4, 5, 7, Schedule.Action.ERRANDS, true);
        }
        if(role == Role.FREERIDER && age > 10) {
            SetActionBlock(schedule, 12, 20, 1, 5, 0, 5, Schedule.Action.ERRANDS, true);
            SetActionBlock(schedule, 10, 18, 0, 2, 5, 7, Schedule.Action.ERRANDS, true);
        }

        //set school or work
        if(role == Role.STUDENT) SetActionBlock(schedule, 8, 8, 7, 7, 0, 5, Schedule.Action.SCHOOL, true);
        if(role == Role.WORKER) SetActionBlock(schedule, 9, 9, 8, 8, 0, 5, Schedule.Action.WORKING, true);

        //set sleep
        int sleepStart = this.schedule.sleepStart;
        int sleepEnd = this.schedule.sleepEnd;
        int sleepTime = 24 - sleepStart;
        SetActionBlock(schedule, 0, 0, sleepEnd, sleepEnd, 0, 7, Schedule.Action.SLEEPING, false);
        SetActionBlock(schedule, sleepStart, sleepStart, sleepTime, sleepTime, 0, 7, Schedule.Action.SLEEPING, false);
    }

    void SetActionBlock(Schedule.Action[,] schedule, int startMin, int startMax, int spaceMin, int spaceMax, int dayStart, int dayEnd, Schedule.Action action, bool rerandomize = false){
        int start = Random.Range(startMin, startMax);
        int space = Random.Range(spaceMin, spaceMax);
        for(int i = dayStart; i < dayEnd; i++){
            for(int j = start; j < start+space; j++){
                schedule[i,j] = action;
            }
            start = Random.Range(startMin, startMax);
            space = Random.Range(spaceMin, spaceMax);
        }
    }

    void GenerateName()
    {
        int chunks = Random.Range(2, 4);
        name += leadCharacter[Random.Range(0, leadCharacter.Length)];
        for (int i = 0; i < chunks; i++)
        {
            int start = Random.Range(0, nameChunk.Length - 3);
            name += nameChunk.Substring(start, 3);
        }
    }

    void FindRole()
    {
        role = Role.FREERIDER;
        if(age >= 5)
        {
            role = Role.STUDENT;
        }
        if (age > 23)
        {
            float freeriderChance = Mathf.Pow((age / 78f), 4f);
            role = Role.WORKER;
            if (Random.Range(0f, 1f) <= freeriderChance)
            {
                role = Role.FREERIDER;
            }
        }
    }
}

[System.Serializable]
public class Schedule
{
    public enum Action
    {
        FREETIME,
        ERRANDS,
        HOME,
        SCHOOL,
        WORKING,
        SLEEPING
    }

    public Action[,] weeklyRoutine;
    public int sleepStart;
    public int sleepEnd;
    public int sleepingHours;

    public Schedule(Action[,] weeklyRoutine, int sleepStart, int sleepEnd, int sleepingHours)
    {
        this.weeklyRoutine = weeklyRoutine;
        this.sleepStart = sleepStart;
        this.sleepEnd = sleepEnd;
        this.sleepingHours = sleepingHours;
    }
}