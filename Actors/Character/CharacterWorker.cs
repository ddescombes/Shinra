﻿using Akka.Actor;
using Akka.Event;
using Shinra.Clients;
using Shinra.Messages;
using Shinra.Messages.Character;
using System;

namespace Shinra.Actors.Character
{
    public class CharacterWorker : ReceiveActor
    {
        private UpdateCharacterMessage _characterMessage;
        private readonly IActorRef _supervisor;
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private readonly IXIVAPIClient _client;
        public CharacterWorker(IActorRef supervisorRef, IXIVAPIClient client)
        {
            _supervisor = supervisorRef;
            _client = client;
            BecomeReady();
        }
        protected override void PreStart() { }
        private void Ready()
        {
            Receive<UpdateCharacterMessage>(message => ProcessNotifyCandidateMessage(message));
            ReceiveAny(message => _logger.Warning("Unhandeled Message while ready {@message}", message));
        }
        private void BecomeReady()
        {
            Become(Ready);
            _supervisor.Tell(new WorkerAvailable());
        }
        protected override void PreRestart(Exception reason, object message)
        {
            UpdateCharacterMessage characterMessage = message as UpdateCharacterMessage;
            _logger.Error(reason, "Error processing candidate @CharacterID", characterMessage.CharacterID);
            base.PreRestart(reason, message);
        }
        private void Busy()
        {
            Receive<CharacterUpdated>(message => BecomeReady());
            Receive<FailureMessage>(message => BecomeReady());
            ReceiveAny(message => _logger.Warning("Unhandeled Message while busy {@message}", message));
        }

        void ProcessNotifyCandidateMessage(UpdateCharacterMessage message)
        {
            Become(Busy);
            _characterMessage = message;
            _client.GetCharacter(message.CharacterID).PipeTo(Self, 
                success: (successMessage) => new CharacterUpdated(successMessage),
                failure: (ex) => new FailureMessage(ex));
        }
    }
}