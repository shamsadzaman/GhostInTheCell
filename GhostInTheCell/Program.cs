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

    /// TODO:
    /// grab neutral factory prod 0 and increase production

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

    private static decimal ArmyThresholdFraction
    {
        get { return 0.25M; }
    }

    public int[][] FactoryDistance;
    public List<FactoryDetail> FactoryDetailList;
    public List<Troop> EnRouteTroopList;                // Troops that are travelling
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

        for (var i = 0; i < linkCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            var factory1 = int.Parse(inputs[0]);
            var factory2 = int.Parse(inputs[1]);
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
        }

        //DebugMessage($"distance from {factory1} to {factory2}: {factoryDistances[1][2]}");

        var player = new Player
        {
            FactoryDistance = factoryDistances,
            NumberOfBombAvailable = 2,
            NumberOfTurn = 0,
            BombedFactoryList = new List<BombedFactory>()
        };

        // game loop
        while (true)
        {
            player.NumberOfTurn++;

            var entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            //DebugMessage("entity count: " + entityCount);

            player.FactoryDetailList = new List<FactoryDetail>();
            player.EnRouteTroopList = new List<Troop>();
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
                        ProductionRate = arg3,
                        DistanceToOtherFactories = factoryDistances[entityId]
                    });
                }
                else if (entityType == "TROOP")
                {
                    player.EnRouteTroopList.Add(new Troop
                    {
                        EntityId = entityId,
                        Attacker = arg1,
                        SourceFactory = arg2,
                        TargetFactory = arg3,
                        NumberOfCyborg = arg4,
                        RemainingTurnToTarget = arg5
                    });

                    //DebugMessage(inputs[0] + ' ' + inputs[1] + ' ' + inputs[2] + ' ' + inputs[3] + ' ' + inputs[4] + ' ' +
                    //         inputs[5] + ' ' + inputs[6]);
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
            var sb = new StringBuilder();

            player.Strategize(sb); // creates the troop list to send

            player.SendCommand(sb);
        }
    }

    /// <summary>
    /// creates the string command
    /// </summary>
    /// <param name="sb"></param>
    public void SendCommand(StringBuilder sb)
    {
        if (TroopListToSend != null && TroopListToSend.Any())
        {
            foreach (var troopToSend in TroopListToSend)
            {
                sb.AppendFormat("MOVE {0} {1} {2};", troopToSend.SourceFactory, troopToSend.TargetFactory,
                    troopToSend.NumberOfCyborg);
            }
        }

        if (sb.Length > 0)
        {
            sb.Length -= 1;
            Console.WriteLine(sb.ToString());
        }
        else
        {
            Console.WriteLine("WAIT");
        }
    }

    private void IncreaseProduction(StringBuilder sb)
    {
        var factoryList =
            FactoryDetailList.Where(x => x.Owner == Owner.Me && x.NumberOfCyborgPresent > 10 && x.ProductionRate < 3)
                .ToList();

        if (!factoryList.Any())
            return;

        foreach(var factory in factoryList)
        {
            if (!IsFactoryUnderAttack(factory.EntityId) ||
                (decimal) GetEnemyTroopSize(factory) / factory.NumberOfCyborgPresent < 0.1m)
            {
                sb.AppendFormat("INC {0};", factory.EntityId);
                UpdateCyborgNumberInFactory(new Troop {SourceFactory = factory.EntityId, NumberOfCyborg = 10});      // creating dummy troop to decrease army size
            }
        }
    }

    private int GetEnemyTroopSize(FactoryDetail attackedFactory)
    {
        var sum = EnRouteTroopList.Where(x => x.EntityId == attackedFactory.EntityId).Sum(x => x.NumberOfCyborg);
        DebugMessage($"attacked factory: {attackedFactory.EntityId} enemyTroop: {sum} factoryTroop: {attackedFactory.NumberOfCyborgPresent}");

        return sum;
    }

    private bool IsFactoryUnderAttack(int factoryEntityId, int ownaterId = Owner.Me)
    {
        var attackerId = ownaterId == Owner.Me ? Owner.Enemy : Owner.Me;
        return EnRouteTroopList.Any(x => x.TargetFactory == factoryEntityId && x.Attacker == attackerId);
    }

    /// <summary>
    /// Assuming given factory Id is under attack
    /// </summary>
    /// <param name="factoryEntityId"></param>
    /// <param name="factoryOwnerId">owner of the given factory</param>
    /// <returns></returns>
    private bool IsFactorySafeAfterAttack(int factoryEntityId, int factoryOwnerId = Owner.Me)
    {
        if (!IsFactoryUnderAttack(factoryEntityId, factoryOwnerId))
        {
            return true;
        }

        var targetFactory = FactoryDetailList.Single(x => x.EntityId == factoryEntityId);

        var troop = EnRouteTroopList.First(x => x.TargetFactory == factoryEntityId);

        DebugMessage($"******Under attack" +
                     $"\nProd Rate: {targetFactory.ProductionRate}" +
                     $"\nCyborg : {targetFactory.NumberOfCyborgPresent}" +
                     $"\ntroop size: {troop.NumberOfCyborg}" + 
                     $"\nturn remaining: {troop.RemainingTurnToTarget}");

        // by the time troop reaches the factory check if the factory would have produced enough cyborg to defeat the troop
        return troop.NumberOfCyborg <=
               targetFactory.ProductionRate * troop.RemainingTurnToTarget + targetFactory.NumberOfCyborgPresent - 10;
    }


    ///todo: if my troop is already attacking a factory and gonna reach there before bomb, don't send the bomb
    private void SendBomb(StringBuilder sb)
    {
        if (NumberOfBombAvailable > 0)
        {
            var enemyMostProductiveFactory =
                FactoryDetailList.FirstOrDefault(x => x.Owner == Owner.Enemy && x.ProductionRate == 3);
                                                //.OrderByDescending(x => x.ProductionRate).First();

            //if (enemyMostProductiveFactory.ProductionRate != 3 && FactoryDetailList.All(x => x.ProductionRate >= 2))
            //    return;

            if (enemyMostProductiveFactory == null)
            {
                return;
            }

            var closestFactory = FindClosestFactory(enemyMostProductiveFactory);
            var distance = FactoryDistance[closestFactory.EntityId][enemyMostProductiveFactory.EntityId];

            DebugMessage(@"******************Distance to target factory (bomb): " + distance
                        + "\nTarget Factory: " + enemyMostProductiveFactory.EntityId
                        + "\nSource Factory: " + closestFactory.EntityId);

            var bombedFactory = BombedFactoryList.FirstOrDefault(x => x.EntityId == enemyMostProductiveFactory.EntityId);
            var shouldSendBomb = false;

            if (bombedFactory == null)
            {
                // was never sent, so send bomb
                shouldSendBomb = true;
            }
            else if (NumberOfTurn - bombedFactory.NumberOfTurnBombWasSent >= 5)
            {
                shouldSendBomb = true;
            }

            if (!shouldSendBomb)
                return;

            sb.AppendFormat("BOMB {0} {1};", closestFactory.EntityId, enemyMostProductiveFactory.EntityId);

            NumberOfBombAvailable--;

            var bomb = new BombedFactory
            {
                EntityId = enemyMostProductiveFactory.EntityId,
                FactoryOwner = -1,
                NumberOfTurnBombWasSent = NumberOfTurn,
                NumberOfTurnBombWillExplode = NumberOfTurn + distance
            };

            BombedFactoryList.Add(bomb);

            DebugMessage($"Sending bomb from: {closestFactory.EntityId} to: {enemyMostProductiveFactory.EntityId} distance: {distance} current turn: {NumberOfTurn} explosion at: {bomb.NumberOfTurnBombWillExplode}");
        }
    }

    public void Initialize()
    {
        TroopListToSend = new List<Troop>();
    }

    public void Strategize(StringBuilder sb)
    {
        IncreaseProduction(sb);

        SendBomb(sb);

        var myFactoriesWithArmiesOverThreshold =
            FactoryDetailList.Where(x => x.Owner == Owner.Me && x.NumberOfCyborgPresent > ArmyThreshold).ToList();

        DebugMessage($"army threshold: {ArmyThreshold}");

        if (myFactoriesWithArmiesOverThreshold.Any())
        {
            DebugMessage("factory over thresh count: " + myFactoriesWithArmiesOverThreshold.Count());

            // todo: select target factory around each of my factory
            foreach(var myFactory in myFactoriesWithArmiesOverThreshold)
            {
                BuildTroopToSendAround(myFactory, Owner.Neutral);
            }

            foreach (var myFactory in myFactoriesWithArmiesOverThreshold)
            {
                BuildTroopToSendAround(myFactory, Owner.Enemy);
            }
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

        SendTroopToNonProdFactory();

        //DefendFactory();


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

    private void SendTroopToNonProdFactory()
    {
        var nonProdFactory = FactoryDetailList.FirstOrDefault(x => x.ProductionRate == 0);

        if (nonProdFactory == null)
        {
            return;     // no prod 0 left
        }

        var myFactories = FactoryDetailList.Where(x => x.Owner == Owner.Me).ToList();

        if (myFactories.Any(x => x.NumberOfCyborgPresent < 20) && myFactories.Any(x => x.ProductionRate < 3) && MyArmySize < EnemyArmySize)
        {
            return;         // check if all my factory has over 20 cyborgs
        }

        BuildTroopList(nonProdFactory);
    }

    private void BuildTroopList(FactoryDetail targetFactory)
    {
        var troopToSend = BuildTroop(targetFactory);

        if (troopToSend != null)
        {
            TroopListToSend.Add(troopToSend);
        }
    }

    private void DefendFactory()
    {
        foreach (var enemyTroop in EnRouteTroopList.Where(x => x.Attacker == Owner.Enemy))
        {
            var attackedFactory = FactoryDetailList.Single(x => x.EntityId == enemyTroop.TargetFactory);

            if (!IsFactorySafeAfterAttack(attackedFactory.EntityId) && FactoryDetailList.Count(x => x.Owner == Owner.Me) > 1 && attackedFactory.ProductionRate > 0)
            {

                var factoryToSkip = new List<int>();

                // send troop from another factory
                var sourceFactory = FindClosestFactory(attackedFactory, true);

                // find a source factory; if that's under attak find the next best one. 
                // then calculate how many troop needs to be sent to defend that factory
                if (IsFactoryUnderAttack(sourceFactory.EntityId))
                {
                    factoryToSkip.Add(sourceFactory.EntityId);
                }
            }

            var tr = TroopListToSend.FirstOrDefault(myTroop => myTroop.SourceFactory == enemyTroop.TargetFactory);

            if (tr != null)
            {
                TroopListToSend.Remove(tr);
                DebugMessage($"Troop removed: {tr.SourceFactory} target: {tr.TargetFactory}");
            }
        }
    }

    private void BuildTroopToSendAround(FactoryDetail sourceFactory, int targetOwnerId)
    {
        var targetFactories = FactoryDetailList.Where(x => x.Owner == targetOwnerId);

        foreach (var nFactory in targetFactories)
        {
            nFactory.AttackValue = (decimal) nFactory.ProductionRate / MaximumProduction +
                                   (1 -
                                    (decimal) sourceFactory.DistanceToOtherFactories[nFactory.EntityId] /
                                    MaximumDistance);
        }

        var targetFactoriesOrderedByAttackValue = targetFactories.OrderByDescending(x => x.AttackValue);

        foreach(var nFactory in targetFactoriesOrderedByAttackValue)
        {
            if(nFactory.NumberOfCyborgPresent < sourceFactory.NumberOfCyborgPresent - 2)
            {
                DebugMessage(
                    $"Sending troop to {nFactory.EntityId} from {sourceFactory.EntityId} source army: {sourceFactory.NumberOfCyborgPresent} targetProdrate: {nFactory.ProductionRate} " +
                    $"distance: {FactoryDistance[sourceFactory.EntityId][nFactory.EntityId]} attack value: {nFactory.AttackValue}");
                AddTroopToSendList(sourceFactory, nFactory);
            }
        }
    }

    private void AddTroopToSendList(FactoryDetail sourceFactory, FactoryDetail targetFactory)
    {
        TroopListToSend.Add(BuildTroop(sourceFactory, targetFactory));
    }

    private Troop BuildTroop(FactoryDetail sourceFactory, FactoryDetail targetFactory)
    {
        var troopToSend = new Troop
        {
            SourceFactory = sourceFactory.EntityId,
            TargetFactory = targetFactory.EntityId,
            NumberOfCyborg = targetFactory.NumberOfCyborgPresent + 2
        };

        sourceFactory.NumberOfCyborgPresent -= troopToSend.NumberOfCyborg;

        DebugMessage($"source factory size: {sourceFactory.NumberOfCyborgPresent}");
        DebugMessage($"from factory list size: {FactoryDetailList.Single(x => x.EntityId == sourceFactory.EntityId).NumberOfCyborgPresent}");
        //UpdateCyborgNumberInFactory(troopToSend);

        return troopToSend;
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

        if (closestFactoryWithBiggerArmy == null)
            return null;

        var troopToSend = new Troop 
        {
            SourceFactory = closestFactoryWithBiggerArmy.EntityId,
            TargetFactory = targetFactory.EntityId,
            NumberOfCyborg = targetFactory.NumberOfCyborgPresent + 2
        };

        UpdateCyborgNumberInFactory(troopToSend);

        return troopToSend;
    }

    private void UpdateCyborgNumberInFactory(Troop troopToSend)
    {
        DebugMessage($"updating factory: {troopToSend.SourceFactory} old army size: {FactoryDetailList.Single(x => x.EntityId == troopToSend.SourceFactory).NumberOfCyborgPresent}");
        FactoryDetailList.Single(x => x.EntityId == troopToSend.SourceFactory).NumberOfCyborgPresent -= troopToSend.NumberOfCyborg;
        DebugMessage($"updating factory: {troopToSend.SourceFactory} new army size: {FactoryDetailList.Single(x => x.EntityId == troopToSend.SourceFactory).NumberOfCyborgPresent}");
    }

    public FactoryDetail FindClosestFactoryWithBiggerArmy(FactoryDetail targetFactory)
    {
        DebugMessage("Target: " + targetFactory.EntityId);

        var distancesFromTargetFactory = FactoryDistance[targetFactory.EntityId];

        var myFactories = FactoryDetailList.Where(x => x.Owner == 1).ToList();

        if (!myFactories.Any())
            return null;

        var minDistance = 30;
        FactoryDetail factoryDetail = null;

        //todo - improvement: I might be able to get rid of the loop if I add the destination array to each object for each factory
        // gets the closest factory
        foreach (var myFactory in myFactories)
        {
            if (distancesFromTargetFactory[myFactory.EntityId] < minDistance &&
                targetFactory.NumberOfCyborgPresent < myFactory.NumberOfCyborgPresent - 2)
            {
                if (IsFactoryUnderAttack(myFactory.EntityId))
                {
                    DebugMessage($"Factory under attack, don't send troop. factory id: {myFactory.EntityId}");
                }
                else
                {
                    minDistance = distancesFromTargetFactory[myFactory.EntityId];
                    factoryDetail = myFactory;
                    DebugMessage(
                        $"Source Factory found: {factoryDetail.EntityId}  army: {factoryDetail.NumberOfCyborgPresent} prod rate: {factoryDetail.ProductionRate} distance: {minDistance}");
                }
            }
        }

        return factoryDetail;
    }

    private static void DebugMessage(string message)
    {
        Console.Error.WriteLine(message);
    }

    private FactoryDetail FindClosestFactory(FactoryDetail targetFactory, bool checkForAttack = false)
    {
        var distancesFromTargetFactory = FactoryDistance[targetFactory.EntityId];

        var myFactories = FactoryDetailList.Where(x => x.Owner == Owner.Me).ToList();

        if (!myFactories.Any())
            return null;

        var minDistance = 30;
        FactoryDetail closestFactory = null;

        //todo - improvement: I might be able to get rid of the loop if I add the destination array to each object for each factory
        // gets the closest factory
        foreach (var myFactory in myFactories)
            if (distancesFromTargetFactory[myFactory.EntityId] < minDistance && targetFactory.EntityId != myFactory.EntityId)
            {
                if (checkForAttack && IsFactoryUnderAttack(myFactory.EntityId))
                {
                    continue;
                }

                minDistance = distancesFromTargetFactory[myFactory.EntityId];
                closestFactory = myFactory;
            }

        return closestFactory;
    }

    #region classes
    public class BombedFactory
    {
        public int EntityId { get; set; }

        public int FactoryOwner { get; set; }

        public int NumberOfTurnBombWasSent { get; set; }

        public int NumberOfTurnBombWillExplode { get; set; }
    }

    public class FactoryDetail
    {
        public int EntityId { get; set; }

        public int Owner { get; set; }

        public int NumberOfCyborgPresent { get; set; }

        public int ProductionRate { get; set; }
        public int[] DistanceToOtherFactories { get; internal set; }

        public decimal AttackValue { get; set; }
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