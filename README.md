# StrawberryJam2021
Code mod to be used for the Celeste Community Strawberry Jam Collab 2021.

## Setup instructions

Feel free to ask a coding team captain in the collab Discord server for help.

Developing directly on the main branch is highly discouraged.

### GUI
- Clone the StrawberryJam2021 repo into your Mods folder.
  - If necessary, the URL is: `git@github.com:StrawberryJam2021/StrawberryJam2021.git`
- You should now have a StrawberryJam2021 folder in your Mods folder.
- Open the `.sln` in VS.
  - Alternatively, use another IDE. If necessary, use the following command: `dotnet build`

### CLI
- `cd` to your `Mods` folder.
- `git clone git@github.com:StrawberryJam2021/StrawberryJam2021.git`
- Open the `.sln` in VS.
  - Alternatively, use another IDE. If necessary, use the following command: `dotnet build`


## How to contribute

- Clone and set up the repo as shown above.
- If you see a request in #coding-requests on discord that you'd like to work on, talk to a modding captain about it. You can discuss the implementation or idea with them and work out the details, together with the person who submitted the request. Once they approve and you know roughly what to do, you can continue with the next step.
- [Create an issue](https://github.com/StrawberryJam2021/StrawberryJam2021/issues/new). This step is important for the captains to keep track of which requests are being worked on.
  - In the title field, you put a short description of the feature (e.g. "Diagonally Moving Kevin Block").
  - In the large comment field, describe the request as you worked it out on discord, as well as any implementation details you discussed. **Make sure to mention the person who originally submitted the request!**
  - In the column to the right of those fields, make sure to _assign the issue to yourself_. This, again, helps captains keep track of who is doing what. Click on "Assignees", then click on your github username or search for it.
  - Click "Submit new issue". This will create the issue and redirect you to it. To the right of the title near the top, you will see a number. This is the issue number, and you need to remember it to refer back to it later.
  - At this point the captain will delete the request from the discord channel and it will be considered "In progress".
- Create a new feature branch on this repository. Give it a name containing your username followed by a (very) short description of the feature (e.g. "octocat/diagonal-kevin"). The style with the slash between the two is recommended.
  - In the CLI, you do this by switching to the main branch, making sure it is up-to-date, and creating a new branch from there.
  ```
  git checkout main
  git pull
  git checkout -b octocat/diagonal-kevin
  ```
  - In the GUI client, you can [follow this guide](https://docs.github.com/en/desktop/contributing-and-collaborating-using-github-desktop/managing-branches#creating-a-branch) to do the same. Make sure you base the new branch on the main branch, and make sure you pulled from main before doing this!
- Code!
  - Implement the feature as it's requested and ask any question you might have in the coding channels on discord. If you are less experienced with modding, don't be shy here. There is no such thing as a stupid question, after all.
  - If, half-way through implementing the code, you realize you're missing some information, it's obviously fine to just ask the captains and the person who originally submitted the request for help.
  - If you're making a custom entity or trigger, make sure to follow those naming conventions:
    - The Ahorn module name (first line in the plugin) should be: `SJ2021YourEntityName` (for example `SJ2021DiagonalKevin`)
    - The displayed name for your entity in Ahorn should be: `Your Entity Name (Options if multiple placements are present) (Strawberry Jam 2021)`. For example `Diagonal Kevin (All Directions) (Strawberry Jam 2021)`
    - The ID of your entity, in both your Ahorn plugin and your `[CustomEntity]` attribute, should be: `SJ2021/YourEntityName` (for example `SJ2021/DiagonalKevin`)
- While coding, commit your changes. This can be done multiple times throughout the implementation process. A commit and push will save any changes you made to the repository online.
  - In the CLI, you have to commit and push your changes to the repository first. For committing, you need a short descriptive message explaining what you changed. For pushing, you need to mention the name of your feature branch, so that it can be created on the repository online.
  ```
  git add --all
  git commit -m "Add Diagonal Kevin Block"
  git push -u origin octocat/diagonal-kevin
  ```
  - In the GUI client, you can [follow this guide](https://docs.github.com/en/desktop/contributing-and-collaborating-using-github-desktop/committing-and-reviewing-changes-to-your-project#2-selecting-changes-to-include-in-a-commit) to do the same. Make sure to push as it says at the very end.
- Once you are done with the implementation, [submit a pull request](https://github.com/StrawberryJam2021/StrawberryJam2021/compare)!
  - Set the `base` branch to `main` and the `compare` branch to the name of your feature branch.
  - Click on "Create pull request".
  - This window will look similar to the issue creation window. Once again, give it a descriptive title; by default, it will use the title of the first commit you made, which is probably good enough if your commit message was good.
  - In the comment field, add anything else you might have to say.
  - At the end of your comment, you should add a keyword that will allow github to automatically close the issue you originally opened when your feature is added to the main branch of the repository. To do this, you need the number of the issue you created. Add the following text to the bottom of your comment (15 in this example is the number of the issue to be closed):
  ```
  Closes #15.
  ```
  - You don't need to assign yourself this time. This is all you need. Press "Create pull request".
- Wait for feedback! If someone requests changes to your implementation, you can just keep changing the code on this branch, and commit and push your changes. You will not have to make a new pull request, you only ever need a single one for each feature request.
- Once a captain approves of it, it will be merged and you are done. Thank you for your contribution!
- Once you submitted your pull request, you can start working on your next feature. For that, all you need to do is follow this guide again from the start. Make a new feature branch for a separate feature. Should changes on a feature be requested, you will need to switch back to the respective feature branch to do those changes, as all features should be completely separate.
- Keep this in mind: One feature per pull request, and one pull request per feature! Don't implement more than one request at once, otherwise it will just get confusing.
