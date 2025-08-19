Wire NGO + Relay to the existing ITransport port.
- Add NgoRelayTransportAdapter that implements ITransport via UnityTransport.
- Keep LocalTransport for SP.
- Provide a small UI for Host/Join (already in RelayBootstrap). Ensure WebGL uses wss.
- Add tests for InputCommand/ActorState blittable serialization.