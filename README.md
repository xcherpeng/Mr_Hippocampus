# Mr_Hippocampus: A Ball Collecting Robot Using Machine Learning

## Problem
For our final COGS 300 project, our task was to design a robot using machine learning to compete in a class-wide tournament. In each round, two opposing robots would compete 
to collect the most amount of balls in 2 minutes. 

## Project Team
I was on a team of 4 people from my lab section; we each had varying levels of programming experience. Since I was one of the 4 people who had some programming experience 
prior, I took the lead of the software engineering side of the project along with another teammate. Our other teammates were in charge of 
design (Robot body and Video).

To begin the project, I coordinated with my team using Google Docs to design a strategy based on what we knew and what resources we had available to us. I
took meeting notes and made sure to understand the vary perspectives of my teammates in order to develop our plan.

## What We Knew
- Each round lasted 2 minutes
- There were 10 collectable balls
- The more balls a robot carried, the slower it got
- Shooting a laser can cause an opponent to freeze and drop their balls

## Resources
- Our TA
- Unity
- C#
- VS Code
- **CogsAgent.css**: basic implementation code for a generic agent (no desired behaviour or strategy) that was provided to us 
- Documentation + Google
- Google Docs: for documenting strategy and meeting notes
- Google Drive

## Solution
Knowing our limitations, skillsets, and resources we decided to implement a strategy that involved 3 modes: 

## Robot Performance Strategy

COLLECT:

When Mr. Hippocampus has less than the majority (5 targets) needed to win,
it will collect the nearest targets until it does. It will return to the base
when it has at least three targets when there are no targets at the base. Once there are
at least three targets at the base, it will collect and return at least the minimum remaining balls 
(5 - # balls at base) to get the majority. This is to reduce the number of trips it needs to make.

DEFENSE:

When Mr. Hippocampus has at least 5 targets in its base, it will stay there and
laser the enemy if it approaches. If it is able, it will avoid enemy lasers by
moving whenever the enemy is directly facing it. Otherwise, it will shoot a constant
defensive line of lasers around the base. 

CRIME:

If an enemy is carrying at least one target nearby, Mr. Hippocampus will shoot it. 
If the enemy has the majority of balls and there are no more balls on the floor, 
Mr. Hippocampus will steal from the enemy. 

Mr. Hippocampus will try to dodge if it can; it has punishments for being frozen and dropping
targets.

Strategy implementation is in **Mr_Hippocampus.cs** and the training configuration is in **configuration.yaml**

## Robot Training Strategy

I lead the training our robot. Based on what I learned in class and online, I told my teammates my plan. 
I opted for training Mr_Hippocampus using Behavioural Cloning first by recording a demo with desired behaviour in Unity so that our
robot first had a model to base its behaviour off. Afterwards, I trained Mr_Hippocampus using 
Generative Adversarial Imitation Learning (GAIL), rewarding it for collecting more balls at once and punishing it for 
dropping balls.

To see the final neural network from the training, you can see **Agent.nn**

## Robot Design

Our design team worked closely with us to develop an adorable Mr_Hippocampus model using Unity.

![image](https://user-images.githubusercontent.com/70073029/186763200-95f71ae1-2af0-4cde-94cb-b2528d37463c.png)

## Robot Viability

While our robot was able to collect balls, it ended up collecting 4 balls and staying at base to defend using lasers, as such,
the implementation of our robot was not completely successful. However, this was a fun experience and I learned a lot! :)

## What I Learned 
- Project management: how to prioritize + set deadlines for a team to ensure that all deliverables were completed on time
- Team work + communication skills: collaborating with a cross-functional team and communicating technical concepts to teammates from
non-technical backgrounds
- Programming in C# and using Unity (I've never programmed in C# prior)
- GAIL, Behavioural Cloning

## How Mr_Hippocampus Could Be Improved
- Experiment with a different strategy to maximize speed because carrying 3 balls at once significantly slowed our robot down (making it more 
vulnerable to attacks)
- Experiment with different rewards to improve implementation
