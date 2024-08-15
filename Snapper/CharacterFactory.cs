using System;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Snapper
{
    public static class CharacterFactory
    {
        private static ConstructorInfo? _characterConstructor;

        private static void Initialize()
        {
            _characterConstructor ??= typeof( ICharacter ).GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, null, new[]
            {
                typeof( IntPtr ),
            }, null )!;
        }

        private static ICharacter Character( IntPtr address )
        {
            Initialize();
            return (ICharacter)_characterConstructor?.Invoke( new object[]
            {
                address,
            } )!;
        }

        public static ICharacter? Convert( IGameObject? actor )
        {
            if( actor == null )
            {
                return null;
            }

            return actor switch
            {
                IPlayerCharacter p => p,
                IBattleChara b     => b,
                _ => actor.ObjectKind switch
                {
                    ObjectKind.BattleNpc => Character( actor.Address ),
                    ObjectKind.Companion => Character( actor.Address ),
                    ObjectKind.Retainer  => Character( actor.Address ),
                    ObjectKind.EventNpc  => Character( actor.Address ),
                    _                    => null,
                },
            };
        }
    }

    public static class GameObjectExtensions
    {
        private const int ModelTypeOffset = 0x01B4;

        public static unsafe int ModelType( this IGameObject actor )
            => *( int* )( actor.Address + ModelTypeOffset );

        public static unsafe void SetModelType( this IGameObject actor, int value )
            => *( int* )( actor.Address + ModelTypeOffset ) = value;
    }
}
