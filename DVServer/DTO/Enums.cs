using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DV.Logic.Job
{
    public enum JobType
    {
        Custom = 0,
        ShuntingLoad = 1,
        ShuntingUnload = 2,
        Transport = 3,
        EmptyHaul = 4,
        ComplexTransport = 5
    }

    public enum CargoType
    {
        None = 0,
        Coal = 20,
        IronOre = 21,
        CrudeOil = 40,
        Diesel = 41,
        Gasoline = 42,
        Methane = 43,
        Logs = 60,
        Boards = 61,
        Plywood = 62,
        Wheat = 80,
        Corn = 81,
        Pigs = 100,
        Cows = 101,
        Chickens = 102,
        Sheep = 103,
        Goats = 104,
        Bread = 120,
        DairyProducts = 121,
        MeatProducts = 122,
        CannedFood = 123,
        CatFood = 124,
        SteelRolls = 140,
        SteelBillets = 141,
        SteelSlabs = 142,
        SteelBentPlates = 143,
        SteelRails = 144,
        ScrapMetal = 150,
        ElectronicsIskar = 160,
        ElectronicsKrugmann = 161,
        ElectronicsAAG = 162,
        ElectronicsNovae = 163,
        ElectronicsTraeg = 164,
        ToolsIskar = 180,
        ToolsBrohm = 181,
        ToolsAAG = 182,
        ToolsNovae = 183,
        ToolsTraeg = 184,
        Furniture = 200,
        Pipes = 201,
        ClothingObco = 220,
        ClothingNeoGamma = 221,
        ClothingNovae = 222,
        ClothingTraeg = 223,
        Medicine = 240,
        ChemicalsIskar = 241,
        ChemicalsSperex = 242,
        NewCars = 260,
        ImportedNewCars = 261,
        Tractors = 262,
        Excavators = 263,
        Alcohol = 280,
        Acetylene = 281,
        CryoOxygen = 282,
        CryoHydrogen = 283,
        Argon = 284,
        Nitrogen = 285,
        Ammonia = 286,
        SodiumHydroxide = 287,
        SpentNuclearFuel = 300,
        Ammunition = 301,
        Biohazard = 302,
        Tanks = 320,
        MilitaryTrucks = 321,
        MilitarySupplies = 322,
        EmptySunOmni = 900,
        EmptyIskar = 901,
        EmptyObco = 902,
        EmptyGoorsk = 903,
        EmptyKrugmann = 904,
        EmptyBrohm = 905,
        EmptyAAG = 906,
        EmptySperex = 907,
        EmptyNovae = 908,
        EmptyTraeg = 909,
        EmptyChemlek = 910,
        EmptyNeoGamma = 911,
        Passengers = 1000
    }

    public enum TrainCarType
    {
        NotSet = 0,
        LocoShunter = 10,
        LocoSteamHeavy = 20,
        Tender = 21,
        LocoSteamHeavyBlue = 22,
        TenderBlue = 23,
        LocoRailbus = 30,
        LocoDiesel = 40,
        FlatbedEmpty = 200,
        FlatbedStakes = 201,
        FlatbedMilitary = 202,
        AutorackRed = 250,
        AutorackBlue = 251,
        AutorackGreen = 252,
        AutorackYellow = 253,
        TankOrange = 300,
        TankWhite = 301,
        TankYellow = 302,
        TankBlue = 303,
        TankChrome = 304,
        TankBlack = 305,
        BoxcarBrown = 400,
        BoxcarGreen = 401,
        BoxcarPink = 402,
        BoxcarRed = 403,
        BoxcarMilitary = 404,
        RefrigeratorWhite = 450,
        HopperBrown = 500,
        HopperTeal = 501,
        HopperYellow = 502,
        GondolaRed = 550,
        GondolaGreen = 551,
        GondolaGray = 552,
        PassengerRed = 600,
        PassengerGreen = 601,
        PassengerBlue = 602,
        HandCar = 700,
        CabooseRed = 750,
        NuclearFlask = 800
    }
}

namespace UnityEngine
{


}

