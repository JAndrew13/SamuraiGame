using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.Services;
using SharedLibrary;
using SharedLibrary.Requests;

namespace Server.Controllers;

[Authorize] // This requries the user to be authenticated with the bearer token
[ApiController]
[Route("[controller]")]
public class HeroController : ControllerBase
{
    private readonly IHeroService _heroService;
    private readonly GameDbContext _context;

    public HeroController(IHeroService playerService, GameDbContext context)
    {
        _heroService = playerService;
        _context = context;
    }

    [HttpGet("{id}")]
    public Hero Get([FromRoute] int id) 
    {
        var player = new Hero() { Id = id};

        _heroService.DoSomething();

        return player;
    }

    [HttpPost("{id}")]
    public IActionResult Edit([FromRoute] int id, [FromBody] CreateHeroRequest request)
    {
        var heroIdsAvailible = JsonConvert.DeserializeObject<List<int>>(User.FindFirst("heroes").Value); // TODO: Fix this!
        if (!heroIdsAvailible.Contains(id)) return Unauthorized();

        var hero = _context.Heroes.First(h => h.Id == id);
        hero.Name = request.Name;

        _context.SaveChanges();

        return Ok();
    }

    [HttpPost]
    public Hero Post(CreateHeroRequest request) 
    {
        var userId = int.Parse(User.FindFirst("id").Value); // This "User" is refering to the claims Identity

        var user = _context.Users.Include(u => u.Heroes).First(u => u.Id == userId); // use this to add hero creation limit
        var hero = new Hero() { 
            Name = request.Name, 
            User = user
        };

        _context.Add(hero);
        _context.SaveChanges();

        hero.User = null; // Remove the user object from the response, deal with this later

        return hero;
    }
    
}

