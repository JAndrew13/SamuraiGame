namespace SharedLibrary;

public class Hero
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }

    public User User { get; set; }
}
