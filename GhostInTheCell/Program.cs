using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{

    public int[][] Factorydistance;
    public List<FactoryDetail> FactoryDetailList;
    public List<Troop> TroopList;

    static void Main(string[] args)
    {
        string[] inputs;
        int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
        Console.Error.WriteLine("number of factories: " + factoryCount);
        Console.Error.WriteLine("Factories: ");
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

            Console.Error.WriteLine(inputs[0] + ' ' + inputs[1] + ' ' + inputs[2]);
        }

        var player = new Player
        {
            Factorydistance = factoryDistances
        };

        // game loop
        while (true)
        {
            int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            Console.Error.WriteLine("Entities: ");

            //var factoryDetails = new int[entityCount, 3];
            player.FactoryDetailList = new List<FactoryDetail>();
            player.TroopList = new List<Troop>();

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
                    player.TroopList.Add(new Troop
                    {
                        EntityId = entityId,
                        Owner = arg1,
                        SourceFactory = arg2,
                        TargetFactory = arg3,
                        NumberOfCyborg = arg4,
                        RemainingTurnToTarget = arg5
                    });
                }

                //Console.Error.WriteLine(inputs[0] + ' ' + inputs[1] + ' ' + inputs[2] + ' ' + inputs[3] + ' ' + inputs[4] + ' ' + inputs[5] + ' ' + inputs[6]);
            }

            //var orderedByProductionRate = factoryDetailList.Where(x => x.Owner != -1).OrderByDescending(x => x.ProductionRate);

            var troopToSend = player.Strategize();

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
            if (troopToSend == null)
            {
                Console.WriteLine("WAIT");
            }
            else
            {
                Console.WriteLine("MOVE {0} {1} {2}", troopToSend.SourceFactory, troopToSend.TargetFactory, troopToSend.NumberOfCyborg);
            }
        }
    }

    public Troop Strategize()
    {
        var productiveNeutralFactories = FactoryDetailList.Where(x => x.Owner == 0).OrderByDescending(x => x.ProductionRate);
        var myFactories = FactoryDetailList.Where(x => x.Owner == 1);
        var enemyFactories = FactoryDetailList.Where(x => x.Owner == -1);

        var troopToSend = new Troop();

        // attack neutral
        if (productiveNeutralFactories != null && productiveNeutralFactories.Any())
        {
            var mostProductiveFactory = productiveNeutralFactories.First();

            return BuildTroop(myFactories, mostProductiveFactory);
        }
        // attack enemy
        else
        {
            var mostProductiveEnemyFactory = enemyFactories.OrderByDescending(x => x.ProductionRate).First();

            return BuildTroop(myFactories, mostProductiveEnemyFactory);
        }

        //return null;
    }

    public Troop BuildTroop(IEnumerable<FactoryDetail> myFactories, FactoryDetail targetFactory)
    {
        // find my closest factory with higher troop
        var closestFactoryWithBiggerArmy = FindClosetFactoryWithBiggerArmy(targetFactory);

        if (closestFactoryWithBiggerArmy != null)
        {
            var troopToSend = new Troop
            {
                SourceFactory = closestFactoryWithBiggerArmy.EntityId,
                TargetFactory = targetFactory.EntityId,
                NumberOfCyborg = targetFactory.NumberOfCyborgPresent + 2
            };

            return troopToSend;
        }

        return null;
    }

    public FactoryDetail FindClosetFactoryWithBiggerArmy(FactoryDetail targetFactory)
    {
        // Factorydistance[targetFactory.EntityId] gives me distance from all the other factories


        //var factoryId = Array.IndexOf(Factorydistance[targetFactory.EntityId], Factorydistance[targetFactory.EntityId].Min());

        var distancesFromTargetFactory = Factorydistance[targetFactory.EntityId];

        var myFactories = FactoryDetailList.Where(x => x.Owner == 1);

        if (myFactories == null || !myFactories.Any())
        {
            // i lost
            return null;
        }

        var minDistance = 30;
        FactoryDetail factoryDetail = null;


        // gets the closest factory
        foreach (var factory in myFactories)
        {

            //try
            //{
                if (distancesFromTargetFactory[factory.EntityId] < minDistance && targetFactory.NumberOfCyborgPresent < factory.NumberOfCyborgPresent)
                {
                    minDistance = distancesFromTargetFactory[factory.EntityId];
                    factoryDetail = factory;
                }
            //}
            //catch
            //{
            //    Console.Error.WriteLine("factory entityId: " + factory.EntityId);
            //    //Console.Error.WriteLine("factory: " + myFactories);
            //    Console.Error.WriteLine("Distance from target: {0}", distancesFromTargetFactory == null);
            //    Console.Error.WriteLine("Distance from target: {0}", distancesFromTargetFactory);
            //    Console.Error.WriteLine("Distance from target: {0}", distancesFromTargetFactory[factory.EntityId] == null);
            //    Console.Error.WriteLine("Target factory: {0}", targetFactory.EntityId);
            //    //Console.Error.WriteLine("My factory: " + targetFactory);

            //    return null;
            //}
        }

        return factoryDetail;
        //distancesFromTargetFactory.

        //var isItMine = FactoryDetailList.Where(x => x.EntityId == factoryId && x.Owner == 1).Any();
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

}