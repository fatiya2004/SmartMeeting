using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Services;
using Xunit;

namespace SmartMeeting.Tests;

public class RoomServiceTests
{
    [Fact]
    public async Task Create_NomDuplique_Echoue()
    {
        await using var context = TestDatabase.Create(nameof(Create_NomDuplique_Echoue));
        var service = new RoomService(context);

        var result = await service.CreateAsync(new RoomRequest { Name = "Salle A", Capacity = 8 });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Create_NouvelleSalle_Reussit()
    {
        await using var context = TestDatabase.Create(nameof(Create_NouvelleSalle_Reussit));
        var service = new RoomService(context);

        var result = await service.CreateAsync(new RoomRequest
        {
            Name = "Salle C",
            Capacity = 20,
            Location = "Bâtiment B"
        });

        Assert.True(result.Success);
        Assert.Equal("Salle C", result.Data!.Name);
    }

    [Fact]
    public async Task GetAll_OnlyActive_FiltreLesSallesDesactivees()
    {
        await using var context = TestDatabase.Create(nameof(GetAll_OnlyActive_FiltreLesSallesDesactivees));
        var service = new RoomService(context);

        await service.UpdateAsync(2, new RoomRequest { Name = "Salle B", Capacity = 4, IsActive = false });

        var actives = await service.GetAllAsync(onlyActive: true);

        Assert.DoesNotContain(actives, r => r.Name == "Salle B");
    }
}
