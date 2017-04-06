using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

// todo: if factory has 1/3 of total army -> pick target -> prefer closer productive ones
// todo: else pick a target -> pick sources
// todo: consider incooming troops
// todo: value production: (productionRate / 3)
// todo: value distance  : (1 - distance / 20)
// todo: attack value = (value production + value distance) -> sort by highest to lowest

//todo: calculate army size for a target factory based on distance and production rate.
//todo: attack CLOSER neutral productive factory first then pick the one with smaller army size 

/*
 * strategy 1: pick a target factory - find the closest factory to send the troops from 
 * - this's what I'm doing right now
 * 
 * strategy 2: pick a source factory - find the closest enemy factory to send the troops to
 * 
 * strategy 3: a combination of strategy 1 & 2?
 */

internal class Player
{
    #region fields

    private const int MaximumDistance = 20;
    private const int MaximumProduction = 3;

    private const decimal ArmyThresholdFraction = 0.333M;

    public int[][] FactoryDistance;
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

    public int TotalArmySize => MyArmySize + EnemyArmySize;

    public decimal ArmyThreshold => TotalArmySize * ArmyThresholdFraction;

    public List<Troop> TroopListToSend { get; private set; }
    public int NumberOfBombAvailable { get; private set; }

    public int NumberOfTurn;                        // Turn number of the game

    #endregion

    private static void Main(string[] args)
    {
        string[] inputs;
        var factoryCount = int.Parse(Console.ReadLine()); // the number of factories
        //Console.Error.WriteLine("number of factories: " + factoryCount);
        //Console.Error.WriteLine("Factories: ");
        var linkCount = int.Parse(Console.ReadLine()); // the number of links between factories

        var factoryDistances = new int[linkCount][];
        int factory1 = 0;
        int factory2 = 0;

        for (var i = 0; i < linkCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            factory1 = int.Parse(inputs[0]);
            factory2 = int.Parse(inputs[1]);
            var distance = int.Parse(inputs[2]);

            if (factoryDistances[factory1] == null)
            {
                factoryDistances[factory1] = new int[linkCount];
            }

            factoryDistances[factory1][factory2] = distance;

            if (factoryDistances[factory2] == null)
            {
                factoryDistances[factory2] = new int[linkCount];
            }

            factoryDistances[factory2][factory1] = distance;        // need to update distance on both side of the array

            //Console.Error.WriteLine(inputs[0] + ' ' + inputs[1] + ' ' + inputs[2]);
            //DebugMessage($"*** distance from {factory1} to {factory2} : {factoryDistances[factory1][factory2]}");
        }

        //for (var i = 0; i < linkCount; i++)
        //{
        //    for (var j = 0; j < linkCount; j++)
        //    {
        //        DebugMessage($"distance from {i} to {j}: {factoryDistances[i][j]}");
        //    }
        //}

        DebugMessage($"distance from {factory1} to {factory2}: {factoryDistances[1][2]}");

        var player = new Player
        {
            FactoryDistance = factoryDistances,
            NumberOfBombAvailable = 2,
            NumberOfTurn = 0,
            BombedFactoryList = new List<BombedFactory>()
        };

        //for (var i = 0; i < linkCount; i++)
        //{
        //    for (var j = 0; j < linkCount; j++)
        //    {
        //        DebugMessage($"distance from {i} to {j}: {player.FactoryDistance[i][j]}");
        //    }
        //}

        DebugMessage($"distance from {factory1} to {factory2}: {player.FactoryDistance[factory1][factory2]}");

        // game loop
        while (true)
        {
            player.NumberOfTurn++;

            var entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            DebugMessage("entity count: " + entityCount);

            player.FactoryDetailList = new List<FactoryDetail>();
            player.TroopListOnRoute = new List<Troop>();
            player.BombListOnRoute = new List<Bomb>();
            player.TroopListToSend = new List<Troop>();

            for (var i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                var entityId = int.Parse(inputs[0]);
                var entityType = inputs[1];
                var arg1 = int.Parse(inputs[2]);
                var arg2 = int.Parse(inputs[3]);
                var arg3 = int.Parse(inputs[4]);
                var arg4 = int.Parse(inputs[5]);
                var arg5 = int.Parse(inputs[6]);

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
                        Attacker = arg1,
                        SourceFactory = arg2,
                        TargetFactory = arg3,
                        NumberOfCyborg = arg4,
                        RemainingTurnToTarget = arg5
                    });

                    DebugMessage(inputs[0] + ' ' + inputs[1] + ' ' + inputs[2] + ' ' + inputs[3] + ' ' + inputs[4] + ' ' +
                             inputs[5] + ' ' + inputs[6]);
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
            }

            //player.Initialize();

            player.Strategize(); // creates the troop list to send

            player.SendCommand();
        }
    }

    /// <summary>
    /// creates the string command
    /// </summary>
    public void SendCommand()
    {
        // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
        if (TroopListToSend == null || !TroopListToSend.Any())
        {
            Console.WriteLine("WAIT");
        }
        else
        {
            var sb = new StringBuilder();

            foreach (var troopToSend in TroopListToSend)
            {
                sb.AppendFormat("MOVE {0} {1} {2};", troopToSend.SourceFactory, troopToSend.TargetFactory, troopToSend.NumberOfCyborg);
            }

            SendBomb(sb);

            IncreaseProduction(sb);

            sb.Length -= 1;
            Console.WriteLine(sb.ToString());
        }
    }

    private void IncreaseProduction(StringBuilder sb)
    {
        var factory = FactoryDetailList.Where(x => x.Owner == 1)
            .FirstOrDefault(x => x.NumberOfCyborgPresent > TotalArmySize / (FactoryDetailList.Count / 2));      // instead of one third army consider the number of factory present in the game to count the threshold to level up production


        if (factory == null || factory.NumberOfCyborgPresent <= 10)
            return;

        var isUnderAttack = IsFactoryUnderAttack(factory.EntityId);
        sb.AppendFormat("INC {0};", factory.EntityId);
    }

    private bool IsFactoryUnderAttack(int factoryEntityId, int ownerId = Owner.Me)
    {
        return TroopListToSend.Any(x => x.TargetFactory == factoryEntityId && x.Attacker == ownerId);
    }

    /// <summary>
    /// Assuming given factory Id is under attack
    /// </summary>
    /// <param name="factoryEntityId"></param>
    /// <param name="ownerId">owner of the given factory</param>
    /// <returns></returns>
    private bool IsFactorySafeAfterAttack(int factoryEntityId, int ownerId = Owner.Me)
    {
        if (!IsFactoryUnderAttack(factoryEntityId, ownerId))
        {
            return true;
        }

        var targetFactory = FactoryDetailList.Single(x => x.EntityId == factoryEntityId);

        var troop = TroopListOnRoute.FirstOrDefault(x => x.EntityId == factoryEntityId);

        if (troop != null)
        {
            // by the time troop reaches the factory check if the factory would have produced enough cyborg to defeat the troop
            return troop.NumberOfCyborg <=
                   targetFactory.ProductionRate * troop.RemainingTurnToTarget + targetFactory.NumberOfCyborgPresent;
        }


        DebugMessage("ERROR: can't find troop");
        return true;
    }


    private void SendBomb(StringBuilder sb)
    {
        if (NumberOfBombAvailable > 0)
        {
            var enemyMostProductiveFactory = FactoryDetailList.Where(x => x.Owner == -1)
                                                .OrderByDescending(x => x.ProductionRate).First();

            if (enemyMostProductiveFactory.ProductionRate != 3 && FactoryDetailList.All(x => x.ProductionRate != 3))
                return;

            var closestFactory = FindClosestFactory(enemyMostProductiveFactory);
            var distance = FactoryDistance[closestFactory.EntityId][enemyMostProductiveFactory.EntityId];

            DebugMessage(@"******************Distance to target factory (bomb): " + distance
                        + "\nTarget Factory: " + enemyMostProductiveFactory.EntityId
                        + "\nSource Factory: " + closestFactory.EntityId);

            //var isBombOnTheWay = BombedFactoryList.Any(x => x.EntityId == enemyMostProductiveFactory.EntityId && NumberOfTurn < x.NumberOfTurnFactoryWasBombed);
            //var shouldTakeFiveTurnsToReachTarget = distance >= 5;

            //var shouldSendBomb = !isBombOnTheWay && shouldTakeFiveTurnsToReachTarget;

            var bombedFactory = BombedFactoryList.FirstOrDefault(x => x.EntityId == enemyMostProductiveFactory.EntityId); // && NumberOfTurn < x.NumberOfTurnFactoryWasBombed);
            var shouldSendBomb = false;

            if(bombedFactory != null && bombedFactory.NumberOfTurnFactoryWasBombed > NumberOfTurn + 5)
            {
                // bomb already on it's way
                //DebugMessage("********* bomb's on the way");
            }
            // this check bumps my rank from 288 to 205; WHY????
            else if (bombedFactory != null && NumberOfTurn + distance - bombedFactory.NumberOfTurnFactoryWasBombed >= 5)
            {
                // bombed in past, send bomb if when bomb reaches production will start again
                shouldSendBomb = true;
            }
            else
            {
                // bomb hasn't been sent
                shouldSendBomb = true;
            }

            if (shouldSendBomb)
                sb.AppendFormat("BOMB {0} {1};", closestFactory.EntityId, enemyMostProductiveFactory.EntityId);

            NumberOfBombAvailable--;


            BombedFactoryList.Add(new BombedFactory
            {
                EntityId = enemyMostProductiveFactory.EntityId,
                FactoryOwner = -1,
                NumberOfTurnFactoryWasBombed = NumberOfTurn + distance
            });
        }
    }

    public void Initialize()
    {
        TroopListToSend = new List<Troop>();
    }

    public void Strategize()
    {
        var myFactoriesWithArmiesOverThreshold = FactoryDetailList.Where(x => x.Owner == Owner.Me && x.NumberOfCyborgPresent > ArmyThreshold);

        if (myFactoriesWithArmiesOverThreshold != null && myFactoriesWithArmiesOverThreshold.Any())
        {
            DebugMessage("factory over thresh count: " + myFactoriesWithArmiesOverThreshold.Count());
        }

        var productiveNeutralFactories = FactoryDetailList.Where(x => x.Owner == 0).Where(x => x.ProductionRate > 0)
            .OrderByDescending(x => x.ProductionRate);

        var enemyFactories = FactoryDetailList.Where(x => x.Owner == -1);

        // attack neutral
        if (productiveNeutralFactories.Any())
            BuildTroopList(productiveNeutralFactories);
        // attack enemy
        else
            BuildTroopList(enemyFactories.OrderByDescending(x => x.ProductionRate));

        DefendFactory();

        // send troop to bombed factory
        //if (BombedFactoryList != null && BombedFactoryList.Any(x => NumberOfTurn - x.NumberOfTurnFactoryWasBombed == 1))
        //{

        //    // factory was bombed last turn, send army to that factory
        //    var bombedFactoryInLastTurnList = BombedFactoryList
        //            .Where(x => NumberOfTurn - x.NumberOfTurnFactoryWasBombed == 1);

        //    Console.Error.WriteLine("Bombed factory count " + bombedFactoryInLastTurnList.Count());


        //    foreach (var bombedFactory in bombedFactoryInLastTurnList)
        //    {
        //        var bombedFactoryDetail = FactoryDetailList
        //                .Where(x => x.EntityId == bombedFactory.EntityId && x.ProductionRate > 1).FirstOrDefault();

        //        if(bombedFactoryDetail == null || TroopListToSend.Any(x => x.TargetFactory == bombedFactoryDetail.EntityId))
        //        {
        //            continue;
        //        }

        //        Console.Error.WriteLine("Factory to bomb: " + bombedFactoryDetail.EntityId);

        //        var troop = BuildTroop(bombedFactoryDetail);

        //        if (troop != null)
        //        {
        //            Console.Error.WriteLine("Adding troop for: " + troop.TargetFactory);
        //            TroopListToSend.Add(troop);
        //        }
        //    }
        //}
    }

    private void DefendFactory()
    {
        //var isUnderAttack = TroopListToSend.Any(x => x.Attacker == -1);

        //TroopListToSend.Remove(TroopListToSend.Single(x => x.EntityId == ));
        foreach (var enemyTroop in TroopListOnRoute.Where(x => x.Attacker == -1))
        {
            //var targetFactoryProductionRate = FactoryDetailList.Single(y => y.EntityId == troop.TargetFactory).ProductionRate;

            //var tr = TroopListToSend.FirstOrDefault(x => x.EntityId == troop.TargetFactory 
            //            && x.NumberOfCyborg + targetFactoryProductionRate * troop.RemainingTurnToTarget < troop.NumberOfCyborg);
            if (IsFactorySafeAfterAttack(enemyTroop.TargetFactory))
            {
                continue;
            }

            var tr = TroopListToSend.FirstOrDefault(myTroop => myTroop.SourceFactory == enemyTroop.TargetFactory);

            if (tr != null)
            {
                TroopListToSend.Remove(tr);
                DebugMessage($"Troop removed: {tr.SourceFactory} target: {tr.TargetFactory}");
            }
        }
    }

    private void BuildTroopList(IOrderedEnumerable<FactoryDetail> productiveFactories)
    {
        foreach (var productiveFactory in productiveFactories)
        {
            var troopToSend = BuildTroop(productiveFactory);

            if (troopToSend != null)
                TroopListToSend.Add(troopToSend);
        }
    }
    
    public Troop BuildTroop(FactoryDetail targetFactory)
    {
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
        DebugMessage("Target: " + targetFactory.EntityId);

        // FactoryDistance[targetFactory.EntityId] gives me distance from all the other factories
        var distancesFromTargetFactory = FactoryDistance[targetFactory.EntityId];

        var myFactories = FactoryDetailList.Where(x => x.Owner == 1);

        //DebugMessage("Number of my factories: " + myFactories.Count());

        if (!myFactories.Any())
            return null;

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
                DebugMessage($"Source Factory found: {factoryDetail.EntityId}  army: {factoryDetail.NumberOfCyborgPresent} prod rate: {factoryDetail.ProductionRate}");
            }
        }

        return factoryDetail;
    }

    private static void DebugMessage(string message)
    {
        Console.Error.WriteLine(message);
    }

    private FactoryDetail FindClosestFactory(FactoryDetail targetFactory)
    {
        var distancesFromTargetFactory = FactoryDistance[targetFactory.EntityId];

        var myFactories = FactoryDetailList.Where(x => x.Owner == 1);

        if (myFactories == null || !myFactories.Any())
            return null;

        var minDistance = 30;
        FactoryDetail factoryDetail = null;

        //todo - improvement: I might be able to get rid of the loop if I add the destination array to each object for each factory
        // gets the closest factory
        foreach (var myFactory in myFactories)
            if (distancesFromTargetFactory[myFactory.EntityId] < minDistance)
            {
                minDistance = distancesFromTargetFactory[myFactory.EntityId];
                factoryDetail = myFactory;
            }

        return factoryDetail;
    }

    #region classes
    public class BombedFactory
    {
        public int EntityId { get; set; }

        public int FactoryOwner { get; set; }

        public int NumberOfTurnFactoryWasBombed { get; set; }
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

        public int Attacker { get; set; }

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

    public class Owner
    {
        public const int Enemy = -1;

        public const int Neutral = 0;

        public const int Me = 1;
    }
    #endregion
}