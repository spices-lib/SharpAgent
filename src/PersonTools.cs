using System;
using System.Collections.Generic;
using System.Text;

public class PersonTools
{
    public string[] GetPersons()
    {
        return GetData();
    }

    public string GetPerson(int personId)
    {
        return personId switch
        {
            1 => "Ben",
            2 => "Susan",
            3 => "Jenny",
            _ => null
        };
    }

    private static string[] GetData()
    {
        return
        [
            "Ben",
            "Susan",
            "Jenny",
        ];
    }

}
