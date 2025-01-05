csharp
using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp;

public class GamblingPlugin : Plugin
{
    private Dictionary<string, Game> games = new Dictionary<string, Game>();
    private EconomyManager economyManager;

    public override void OnPluginLoaded()
    {
        games.Add("roulette", new RouletteGame());
        games.Add("blackjack", new BlackjackGame());
        games.Add("coinflip", new CoinFlipGame());
        games.Add("rockpaperscissors", new RockPaperScissorsGame());
        games.Add("diceroll", new DiceRollGame());
        games.Add("slotmachine", new SlotMachineGame());

        economyManager = new EconomyManager();

        foreach (var game in games.Values)
        {
            game.OnGameLoaded();
        }
    }

    public override void OnClientCommand(Client client, string command, string[] args)
    {
        if (command.StartsWith("!"))
        {
            var gameName = command.Substring(1).ToLower();

            if (games.ContainsKey(gameName))
            {
                games[gameName].OnClientCommand(client, args);
            }
            else if (command == "!balance")
            {
                client.SendMessage($"Your balance: {economyManager.GetBalance(client)}");
            }
            else if (command == "!leaderboard")
            {
                var leaderboard = economyManager.GetLeaderboard();
                client.SendMessage("Leaderboard:");
                foreach (var player in leaderboard)
                {
                    client.SendMessage($"{player.Key}: {player.Value}");
                }
            }
            else if (command == "!help")
            {
                client.SendMessage("Available commands:");
                client.SendMessage("!roulette");
                client.SendMessage("!blackjack");
                client.SendMessage("!coinflip");
                client.SendMessage("!rockpaperscissors");
                client.SendMessage("!diceroll");
                client.SendMessage("!slotmachine");
                client.SendMessage("!balance");
                client.SendMessage("!leaderboard");
                client.SendMessage("!help");
            }
            else
            {
                client.SendMessage("Invalid command.");
            }
        }
    }
}

public abstract class Game
{
    public virtual void OnGameLoaded() { }

    public virtual void OnClientCommand(Client client, string[] args) { }
}

public class RouletteGame : Game
{
    private Random random = new Random();

    public override void OnClientCommand(Client client, string[] args)
    {
        if (args.Length < 2)
        {
            client.SendMessage("Invalid roulette command. Usage: !roulette <bet> <amount>");
            return;
        }

        var bet = args[0].ToLower();
        var amount = int.Parse(args[1]);

        if (bet != "red" && bet != "black" && bet != "even" && bet != "odd" && !int.TryParse(bet, out _))
        {
            client.SendMessage("Invalid roulette bet. Usage: !roulette <bet> <amount>");
            return;
        }

        var winningNumber = random.Next(0, 37);
        var winningColor = winningNumber % 2 == 0 ? "red" : "black";

        client.SendMessage($"Roulette wheel spinning... Winning number: {winningNumber} ({winningColor})");

        if (bet == "red" && winningColor == "red" || bet == "black" && winningColor == "black")
        {
            client.SendMessage($"You won! Payout: {amount * 2}");
            economyManager.AddBalance(client, amount * 2);
        }
        else if (bet == "even" && winningNumber % 2 == 0 || bet == "odd" && winningNumber % 2 != 0)
        {
            client.SendMessage($"You won! Payout: {amount * 2}");
            economyManager.AddBalance(client, amount * 2);
        }
        else if (int.TryParse(bet, out var betNumber) && betNumber == winningNumber)
        {
            client.SendMessage($"You won! Payout: {amount * 35}");
            economyManager.AddBalance(client, amount * 35);
        }
        else
        {
            client.SendMessage("You lost.");
            economyManager.RemoveBalance(client, amount);
        }
    }
}

public class BlackjackGame : Game
{
    private Dictionary<Client, Hand> hands = new Dictionary<Client, Hand>();

    public override void OnClientCommand(Client client, string[] args)
    {
        if (args.Length < 1)
        {
            client.SendMessage("Invalid blackjack command. Usage: !blackjack <hit/stand/double>");
            return;
        }

        var action = args[0].ToLower();

        if (!hands.ContainsKey(client))
        {
            hands[client] = new Hand();
            hands[client].DealInitialCards();
        }

        switch (action)
        {
            case "hit":
                hands[client].Hit();
                break;
            case "stand":
                hands[client].Stand();
                break;
            case "double":
                hands[client].DoubleDown();
                break;
            default:
                client.SendMessage("Invalid blackjack action.");
                return;
        }

        client.SendMessage($"Your hand: {hands[client].ToString()}");

        if (hands[client].IsBlackjack())
        {
            client.SendMessage("You got a blackjack! Payout: 3:2");
            economyManager.AddBalance(client, 3 * 2);
        }
        else if (hands[client].IsBust())
        {
            client.SendMessage("You busted. You lost.");
            economyManager.RemoveBalance(client, 1);
        }
        else if (hands[client].IsStand())
        {
            var dealerHand = new Hand();
            dealerHand.DealInitialCards();

            while (dealerHand.GetValue() < 17)
            {
                dealerHand.Hit();
            }

            client.SendMessage($"Dealer's hand: {dealerHand.ToString()}");

            if (dealerHand.IsBust())
            {
                client.SendMessage("Dealer busted. You won!");
                economyManager.AddBalance(client, 1);
            }
            else if (dealerHand.GetValue() > hands[client].GetValue())
            {
                client.SendMessage("Dealer's hand is higher. You lost.");
                economyManager.RemoveBalance(client, 1);
            }
            else if (dealerHand.GetValue() < hands[client].GetValue())
            {
                client.SendMessage("Your hand is higher. You won!");
                economyManager.AddBalance(client, 1);
            }
            else
            {
                client.SendMessage("It's a tie.");
            }
        }
    }
}

public class CoinFlipGame : Game
{
    private Random random = new Random();

    public override void OnClientCommand(Client client, string[] args)
    {
        if (args.Length < 2)
        {
            client.SendMessage("Invalid coin flip command. Usage: !coinflip <heads/tails> <amount>");
            return;
        }

        var bet = args[0].ToLower();
        var amount = int.Parse(args[1]);

        if (bet != "heads" && bet != "tails")
        {
            client.SendMessage("Invalid coin flip bet. Usage: !coinflip <heads/tails> <amount>");
            return;
        }

        var winningSide = random.Next(2) == 0 ? "heads" : "tails";

        client.SendMessage($"Coin flipping... Winning side: {winningSide}");

        if (bet == winningSide)
        {
            client.SendMessage($"You won! Payout: {amount * 2}");
            economyManager.AddBalance(client, amount * 2);
        }
        else
        {
            client.SendMessage("You lost.");
            economyManager.RemoveBalance(client, amount);
        }
    }
}

public class RockPaperScissorsGame : Game
{
    private Dictionary<Client, string> challenges = new Dictionary<Client, string>();

    public override void OnClientCommand(Client client, string[] args)
    {
        if (args.Length < 2)
        {
            client.SendMessage("Invalid rock-paper-scissors command. Usage: !rockpaperscissors <rock/paper/scissors> <amount>");
            return;
        }

        var choice = args[0].ToLower();
        var amount = int.Parse(args[1]);

        if (choice != "rock" && choice != "paper" && choice != "scissors")
        {
            client.SendMessage("Invalid rock-paper-scissors choice. Usage: !rockpaperscissors <rock/paper/scissors> <amount>");
            return;
        }

        if (args.Length > 2)
        {
            var opponent = args[2];

            if (challenges.ContainsKey(client))
            {
                client.SendMessage("You already have a challenge pending.");
                return;
            }

            challenges[client] = choice;

            client.SendMessage($"Challenge sent to {opponent}.");

            // Handle opponent's response
        }
        else
        {
            var winningChoice = GetWinningChoice(choice);

            client.SendMessage($"You played {choice}. Winning choice: {winningChoice}");

            if (choice == winningChoice)
            {
                client.SendMessage($"You won! Payout: {amount * 2}");
                economyManager.AddBalance(client, amount * 2);
            }
            else
            {
                client.SendMessage("You lost.");
                economyManager.RemoveBalance(client, amount);
            }
        }
    }

    private string GetWinningChoice(string choice)
    {
        switch (choice)
        {
            case "rock":
                return "paper";
            case "paper":
                return "scissors";
            case "scissors":
                return "rock";
            default:
                throw new ArgumentException("Invalid choice.");
        }
    }
}

public class DiceRollGame : Game
{        
    private Random random = new Random();

    public override void OnClientCommand(Client client, string[] args)
    {
        if (args.Length < 2)
        {
            client.SendMessage("Invalid dice roll command. Usage: !diceroll <amount>");
            return;
        }

        var amount = int.Parse(args[1]);

        var roll = random.Next(1, 7);

        client.SendMessage($"Dice rolling... Result: {roll}");

        var opponentRoll = random.Next(1, 7);

        client.SendMessage($"Opponent's roll: {opponentRoll}");

        if (roll > opponentRoll)
        {
            client.SendMessage($"You won! Payout: {amount * 2}");
            economyManager.AddBalance(client, amount * 2);
        }
        else if (roll < opponentRoll)
        {
            client.SendMessage("You lost.");
            economyManager.RemoveBalance(client, amount);
        }
        else
        {
            client.SendMessage("It's a tie.");
        }
    }
}
      economyManager.RemoveBalance(client, amount);
    }
}