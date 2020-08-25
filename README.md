# Colosseum

### Description
Colosseum is a experimental idea to develop a game with focus on competitive gameplay, equal quances and giving the user complete control over the character they develop themselves during the game. As the game induces the style persued in-game will be to deliver an arena-like feel for the player, fighting from round to round, either against another player or against enemies with scripted behavioral patterns.

### Short-term goals
1. More familiarity with the development of projects and games especially
2. Gathering more familiarity with workflows inside the unity environment especially in view of _gameplay programming_
    - Discovering and learning about new packages available in unity:
      - [New input system package](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)
      - New DOTS in unity including
        - [entity package](https://docs.unity3d.com/Packages/com.unity.entities@0.10/manual/index.html)
        - [physics package](https://docs.unity3d.com/Packages/com.unity.physics@0.3/manual/index.html)
      - [New UI development package](https://www.youtube.com/watch?v=t4tfgI1XvGs&t=1838s) 

### Long-term goals
1. Getting more insight into how games can be developed
2. Creating a first try basis on a game developed for multi-player usage including maybe [up to date methods on unity](https://blogs.unity3d.com/2018/08/02/evolving-multiplayer-games-beyond-unet/) (once they are availbe)


### Roadmap

* [x] Woking out simplified game idea
* [x] Deciding on tech stach to use
* [x] Opening [project](https://github.com/CarrotKutay/Colosseum/projects/1) to allocate issues regarding the in game style, art and creational direction
* [x] Creating basic player movement and adding [project](https://github.com/CarrotKutay/Colosseum/projects/2) for allocating issues for it
    * [x] Changing idea to be more focused on physics: [issue #10](https://github.com/CarrotKutay/Colosseum/issues/10) is it possible to move the player only based on physiscs? This could lead to simplifying many problems when animations are implemented on top of in world movement
* [x] Deciding to implement entity-component-system workflow into the project
* [ ] Changing player movemnt to be purely steered by physics command ([helpful orientation](https://www.youtube.com/playlist?list=PLWYGofN_jX5CtphJqxBpn3_HwAyoLjdig))

### Tech-Stack
As this project is for now mostly an experimental one, the tech stack does not need to be fully completed for now.
The basis to perform the experiments on is the unity engine. All gameplay programming will be done there. 

For designíng purposes 3rd party software might be usefull and looking into getting mroe familiar with Bleneder or maybe even ZBrush if time and oppportunity allows could be usefull. Apart from this, most of the design will be done in unity directy, also for more easier compatibility.

Backend: If this experiment ever will go into a phase to require a backend serevr + database, I would be very interested to try out the combination of PostgreSQL + Python
