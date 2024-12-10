await DoWork();

Console.WriteLine("Done!");

static async Task DoWork()
{
    await StepOne();
    await StepTwo();
}

static async Task StepOne()
{
    await Task.Delay(500);
}

static async Task StepTwo()
{
    await Task.Delay(1000);
}