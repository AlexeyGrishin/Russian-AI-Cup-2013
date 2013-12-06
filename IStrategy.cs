using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    //это не мое
    public interface IStrategy
    {
        void Move(Trooper self, World world, Game game, Move move);
    }
}