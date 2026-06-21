namespace HamBusLog.Tests;

using HamBusLog.ViewModels;

public class FieldDayBonusPointsViewModelTests
{
    [Fact]
    public void TotalBonusPoints_NoSelections_ReturnsZero()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel();

        // Act
        var total = vm.GetTotalBonusPoints();

        // Assert
        Assert.Equal(0, total);
    }

    [Fact]
    public void TotalBonusPoints_SingleHundredPointBonus_Returns100()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel { EmergencyPower = true };

        // Act
        var total = vm.GetTotalBonusPoints();

        // Assert
        Assert.Equal(100, total);
    }

    [Fact]
    public void TotalBonusPoints_AllHundredPointBonuses_Returns1200()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel
        {
            EmergencyPower = true,
            MediaPublicity = true,
            PublicLocation = true,
            PublicInfoTable = true,
            MessageToSectionManager = true,
            SatelliteQso = true,
            W1awBulletinCopy = true,
            EducationalActivity = true,
            SocialMedia = true,
            SafetyOfficer = true
        };

        // Act
        var total = vm.GetTotalBonusPoints();

        // Assert
        Assert.Equal(1000, total); // 10 bonuses * 100 points each
    }

    [Fact]
    public void TotalBonusPoints_FormalMessagesOnly_CalculatesCorrectly()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel { FormalMessagesSent = 50 };

        // Act
        var total = vm.GetTotalBonusPoints();

        // Assert
        Assert.Equal(50, total);
    }

    [Fact]
    public void TotalBonusPoints_FormalMessagesMaxedOut_Returns100()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel { FormalMessagesSent = 150 }; // Should be capped at 100

        // Act
        var total = vm.GetTotalBonusPoints();

        // Assert
        Assert.Equal(100, total);
    }

    [Fact]
    public void TotalBonusPoints_YouthParticipation_CalculatesCorrectly()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel { YouthParticipation = 3 }; // 3 youth * 20 points

        // Act
        var total = vm.GetTotalBonusPoints();

        // Assert
        Assert.Equal(60, total);
    }

    [Fact]
    public void TotalBonusPoints_YouthParticipationMaxed_Returns100()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel { YouthParticipation = 10 }; // Should be capped at 5 (100 points)

        // Act
        var total = vm.GetTotalBonusPoints();

        // Assert
        Assert.Equal(100, total);
    }

    [Fact]
    public void TotalBonusPoints_MixedBonuses_CalculatesCorrectly()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel
        {
            EmergencyPower = true,           // 100
            MediaPublicity = true,           // 100
            FormalMessagesSent = 75,         // 75
            YouthParticipation = 2           // 40 (2 * 20)
        };

        // Act
        var total = vm.GetTotalBonusPoints();

        // Assert
        Assert.Equal(315, total); // 100 + 100 + 75 + 40
    }

    [Fact]
    public void TotalBonusPoints_UpdatedProperty_RefreshesDisplay()
    {
        // Arrange
        var vm = new FieldDayBonusPointsViewModel();
        var beforeBonus = vm.TotalBonusPoints;

        // Act
        vm.EmergencyPower = true;
        var afterBonus = vm.TotalBonusPoints;

        // Assert
        Assert.Equal("0", beforeBonus);
        Assert.Equal("100", afterBonus);
    }
}

