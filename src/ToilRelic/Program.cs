using ToilRelic;
using ToilRelic.Systems;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var game = new Game(new SaveSystem());
game.Run();
