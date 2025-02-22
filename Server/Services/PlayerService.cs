namespace Server.Services;

public interface IHeroService {
    void DoSomething();
}

public class PlayerService : IHeroService {
    public void DoSomething() 
    {
        // Might grab players from DB, List Players, etc
        Console.WriteLine("DoSomething, from the PlayerService!");
    }
}