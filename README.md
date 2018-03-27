# Rust-Rcon---Server-Message
Connects to Rust Rcon server and posts death messages back to the server. 

This project is kind of a mesh between https://codereview.stackexchange.com/questions/41591/websockets-client-code-and-making-it-production-ready and https://github.com/aiusepsi/SourceRcon

I wanted to mimic the functionality of Oxide Mod "Death Notes" without having to run mods. So this rcon client listens to Rust and when the server tells Rcon something like RespectableName(1234817239/1234534) was killed by bear (BEAR), this message isn't displayed to users on the server. So my app parses that and returns something like "I come bearing terrible news, RespectableName was just barely killed (bear)" using the server /SAY command. This way when someone dies everyone on the server will see a message from SERVER with that player's shameful death. 

This uses .Net 4.5, and despite some efforts, will not run in Mono. There is some bug in Mono's implementation of WebSocket.ReceiveAsync and sometimes it works and then randomly crashes with an "out of bounds index error", and other times it immediately crashes. 
