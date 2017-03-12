using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;


class Player
{
    public int[][] Factorydistance;
    public List<FactoryDetail> FactoryDetailList;
    public List<Troop> TroopListOnRoute;                // Troops that are travelling
    public List<Bomb> BombListOnRoute;
    public List<BombedFactory> BombedFactoryList;

    public int MyArmySize
    {
        get
        {
            return FactoryDetailList.Where(x => x.Owner == 1).Sum(x => x.NumberOfCyborgPresent);
        }
    }

    public int EnemyArmySize
    {
        get
        {
            return FactoryDetailList.Where(x => x.Owner == -1).Sum(x => x.NumberOfCyborgPresent);
        }
    }

    public List<Troop> TroopListToSend { get; private set; }
    public int NumberOfBombAvailable { get; private set; }

    public int NumberOfTurn;                        // Turn number of the game

    static void Main(string[] args)
    {
        string[] inputs;
        int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
        //Console.Error.WriteLine("number of factories: " + factoryCount);
        //Console.Error.WriteLine("Factories: ");
        int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories

        var factoryDistances = new int[linkCount][];

        for (int i = 0; i < linkCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int factory1 = int.Parse(inputs[0]);
            int factory2 = int.Parse(inputs[1]);
            int distance = int.Parse(inputs[2]);

            factoryDistances[factory1] = new int[linkCount];
            factoryDistances[factory1][factory2] = distance;
            factoryDistances[factory2] = new int[linkCount];
            factoryDistances[factory2][factory1] = distance;        // need to update distance on both side of the array

            //Console.Error.WriteLine(inputs[0] + ' ' + inputs[1] + ' ' + inputs[2]);
        }

        var player = new Player
        {
            Factorydistance = factoryDistances,
            NumberOfBombAvailable = 2,
            NumberOfTurn = 0,
            BombedFactoryList = new List<BombedFactory>()
        };

        // game loop
        while (true)
        {
            player.NumberOfTurn++;

            int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            Console.Error.WriteLine("Number of count: {0}", player.NumberOfTurn);

            player.FactoryDetailList = new List<FactoryDetail>();
            player.TroopListOnRoute = new List<Troop>();
            player.BombListOnRoute = new List<Bomb>();

            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int entityId = int.Parse(inputs[0]);
                string entityType = inputs[1];
                int arg1 = int.Parse(inputs[2]);
                int arg2 = int.Parse(inputs[3]);
                int arg3 = int.Parse(inputs[4]);
                int arg4 = int.Parse(inputs[5]);
                int arg5 = int.Parse(inputs[6]);

                if (entityType == "FACTORY")
                {
                    player.FactoryDetailList.Add(new FactoryDetail
                    {
                        EntityId = entityId,
                        Owner = arg1,
                        NumberOfCyborgPresent = arg2,
                        ProductionRate = arg3
                    });
                }
                else if (entityType == "TROOP")
                {
                    player.TroopListOnRoute.Add(new Troop
                    {
                        EntityId = entityId,
                        Owner = arg1,
                        SourceFactory = arg2,
                        TargetFactory = arg3,
                        NumberOfCyborg = arg4,
                        RemainingTurnToTarget = arg5
                    });
                }
                else if (entityType == "BOMB")
                {
                    player.BombListOnRoute.Add(new Bomb
                    {
                        EntityId = entityId,
                        Owner = arg1,
                        SourceFactory = arg2,
                        TargetFactory = arg3,
                        RemainingTurnToTarget = arg4
                    });
                }

                //Console.Error.WriteLine(inputs[0] + ' ' + inputs[1] + ' ' + inputs[2] + ' ' + inputs[3] + ' ' + inputs[4] + ' ' + inputs[5] + ' ' + inputs[6]);
            }

            player.Initialize();

            player.Strategize(); // creates the troop list to send

            player.SendCommand();
        }
    }

    public void SendCommand()
    {
        // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
        if (TroopListToSend == null || !TroopListToSend.Any())
        {
            Console.WriteLine("WAIT");
        }
        else
        {
            //Console.Error.WriteLine("Troop size: {0}", TroopListToSend.Count);

            var sb = new StringBuilder();

            foreach (var troopToSend in TroopListToSend)
            {
                sb.AppendFormat("MOVE {0} {1} {2};", troopToSend.SourceFactory, troopToSend.TargetFactory, troopToSend.NumberOfCyborg);
            }

            SendBombToMostProductiveFactory(sb);

            IncreaseProduction(sb);

            sb.Length -= 1;
            Console.WriteLine(sb.ToString());
        }
    }

    private void IncreaseProduction(StringBuilder sb)
    {
        var factory = FactoryDetailList.Where(x => x.Owner == 1).FirstOrDefault(x => x.NumberOfCyborgPresent > 50);

        if (factory != null)
        {
            sb.AppendFormat("INC {0};", factory.EntityId);
        }
    }

    private void SendBombToMostProductiveFactory(StringBuilder sb)
    {
        if (NumberOfBombAvailable > 0)
        {
            var enemyFactoryWithBiggestArmy = FactoryDetailList.Where(x => x.Owner == -1).OrderByDescending(x => x.ProductionRate).First();

            var isFactoryAlreadyBombedInLastFiveTurn = BombedFactoryList.Any(x => x.EntityId == enemyFactoryWithBiggestArmy.EntityId && (NumberOfTurn - x.NumberOfTurnWhenItWasBombed) < 5);

            Console.Error.WriteLine("Bombed factory");
            foreach(var bombedFactory in BombedFactoryList)
            {
                Console.Error.WriteLine("{0}  {1}  {2}", bombedFactory.EntityId, bombedFactory.FactoryOwner, bombedFactory.NumberOfTurnWhenItWasBombed);
            }

            if (isFactoryAlreadyBombedInLastFiveTurn)
            {
                return;
            }

            var closestFactory = FindClosestFactory(enemyFactoryWithBiggestArmy);

            sb.AppendFormat("BOMB {0} {1};", closestFactory.EntityId, enemyFactoryWithBiggestArmy.EntityId);

            NumberOfBombAvailable--;

            BombedFactoryList.Add(new BombedFactory
            {
                EntityId = enemyFactoryWithBiggestArmy.EntityId,
                FactoryOwner = -1,
                NumberOfTurnWhenItWasBombed = NumberOfTurn
            });
        }
    }

    

    //todo: calculate army size for a target factory based on distance and production rate.
    //todo: 

    public void Initialize()
    {
        TroopListToSend = new List<Troop>();
    }

    public void Strategize()
    {
        var productiveNeutralFactories = FactoryDetailList.Where(x => x.Owner == 0).OrderByDescending(x => x.ProductionRate);
        var myFactories = FactoryDetailList.Where(x => x.Owner == 1);
        var enemyFactories = FactoryDetailList.Where(x => x.Owner == -1);

        //var troopToSend = new Troop();

        // attack neutral
        if (productiveNeutralFactories != null && productiveNeutralFactories.Any())
        {

            //var mostProductiveFactory = productiveNeutralFactories.First();

            BuildTroopList(myFactories, productiveNeutralFactories);
            
        }
        // attack enemy
        else
        {
            //var mostProductiveEnemyFactory = enemyFactories.OrderByDescending(x => x.ProductionRate).First();

            BuildTroopList(myFactories, enemyFactories.OrderByDescending(x => x.ProductionRate));
        }

        //return null;
    }

    private void BuildTroopList(IEnumerable<FactoryDetail> myFactories, IOrderedEnumerable<FactoryDetail> productiveFactories)
    {
        foreach (var productiveFactory in productiveFactories)
        {
            var troopToSend = BuildTroop(myFactories, productiveFactory);

            if (troopToSend != null)
            {
                TroopListToSend.Add(troopToSend);
            }
        }
    }
    
    public Troop BuildTroop(IEnumerable<FactoryDetail> myFactories, FactoryDetail targetFactory)
    {
        // find my closest factory with higher troop
        var closestFactoryWithBiggerArmy = FindClosestFactoryWithBiggerArmy(targetFactory);

        if (closestFactoryWithBiggerArmy != null)
        {
            var troopToSend = new Troop
            {
                SourceFactory = closestFactoryWithBiggerArmy.EntityId,
                TargetFactory = targetFactory.EntityId,
                NumberOfCyborg = targetFactory.NumberOfCyborgPresent + 2
            };

            UpdateCyborgNumberInFactory(troopToSend);

            return troopToSend;
        }

        return null;
    }

    private void UpdateCyborgNumberInFactory(Troop troopToSend)
    {
        FactoryDetailList.Single(x => x.EntityId == troopToSend.SourceFactory).NumberOfCyborgPresent -= troopToSend.NumberOfCyborg;
    }

    public FactoryDetail FindClosestFactoryWithBiggerArmy(FactoryDetail targetFactory)
    {
        // Factorydistance[targetFactory.EntityId] gives me distance from all the other factories
        var distancesFromTargetFactory = Factorydistance[targetFactory.EntityId];

        var myFactories = FactoryDetailList.Where(x => x.Owner == 1);

        if (myFactories == null || !myFactories.Any())
        {
            // I lost, no factories left
            return null;
        }

        var minDistance = 30;
        FactoryDetail factoryDetail = null;

        //todo - improvement: I might be able to get rid of the loop if I add the destination array to each object for each factory
        // gets the closest factory
        foreach (var myFactory in myFactories)
        {

            if (distancesFromTargetFactory[myFactory.EntityId] < minDistance && targetFactory.NumberOfCyborgPresent < myFactory.NumberOfCyborgPresent)
            {
                minDistance = distancesFromTargetFactory[myFactory.EntityId];
                factoryDetail = myFactory;
            }
        }

        return factoryDetail;
        //distancesFromTargetFactory.

        //var isItMine = FactoryDetailList.Where(x => x.EntityId == factoryId && x.Owner == 1).Any();
    }

    private FactoryDetail FindClosestFactory(FactoryDetail targetFactory)
    {
        var distancesFromTargetFactory = Factorydistance[targetFactory.EntityId];

        var myFactories = FactoryDetailList.Where(x => x.Owner == 1);

        if (myFactories == null || !myFactories.Any())
        {
            // I lost, no factories left
            return null;
        }

        var minDistance = 30;
        FactoryDetail factoryDetail = null;

        //todo - improvement: I might be able to get rid of the loop if I add the destination array to each object for each factory
        // gets the closest factory
        foreach (var myFactory in myFactories)
        {

            if (distancesFromTargetFactory[myFactory.EntityId] < minDistance)
            {
                minDistance = distancesFromTargetFactory[myFactory.EntityId];
                factoryDetail = myFactory;
            }
        }

        return factoryDetail;
    }

    public class BombedFactory
    {
        public int EntityId { get; set; }

        public int FactoryOwner { get; set; }

        public int NumberOfTurnWhenItWasBombed { get; set; }
    }

    public class FactoryDetail
    {
        public int EntityId { get; set; }

        public int Owner { get; set; }

        public int NumberOfCyborgPresent { get; set; }

        public int ProductionRate { get; set; }
    }

    public class Troop
    {
        public int EntityId { get; set; }

        public int Owner { get; set; }

        public int SourceFactory { get; set; }

        public int TargetFactory { get; set; }

        public int NumberOfCyborg { get; set; }

        public int RemainingTurnToTarget { get; set; }
    }

    public class Bomb
    {
        public int EntityId { get; set      ; }

        public int Owner { get; set; }

        public int SourceFactory { get; set; }

        public int TargetFactory { get; set; }

        public int RemainingTurnToTarget { get; set; }
    }
}