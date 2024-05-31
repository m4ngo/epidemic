using System.Collections.Generic;
using UnityEngine;

public class EpidemicDirector : MonoBehaviour
{
    const float CONSTANT = (3.61259412f * 0.0000000000000000001f);

    [Header("Simulation Start")]
    [SerializeField] private int residentCount;

    [Header("Resident Data")]
    [SerializeField] private List<Resident> residents = new List<Resident>();

    private void Start()
    {
        for (int i = 0; i < residentCount; i++)
        {
            int age = AgeDistribution(Random.Range(1f, 100f));
            residents.Add(new Resident(age));
        }
        PrintSchedule(residents[0]);
    }

    void PrintSchedule(Resident resident)
    {
        for (int i = 0; i < 7; i++)
        {
            string output = "[";
            for (int j = 0; j < 24; j++)
            {
                output += resident.schedule.weeklyRoutine[i,j].ToString().Substring(0,2) + ", ";
            }
            output += "]";
            print(output);
        }
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
    public static string nameChunk = "asteriosmaximilianbrunhildelisaalyssadomitillaseppstyopasakchainataliyaprasadfarrahendricus";
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
        if(age > 5)
        {
            role = Role.STUDENT;
        }
        if (age > 20)
        {
            float freeriderChance =  Mathf.Pow((age / 75f), 4f);
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
        SLEEPING,
        FREETIME,
        WORKING,
        SCHOOL,
        ERRANDS,
        HOME
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