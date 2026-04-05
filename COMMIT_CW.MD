Mise à jour des contextes et commit
Exécuter dans l'ordre :

base : résultat du brief précédant
Mettre à jour CLAUDE.md avec les détails d'implémentation des chantiers de cette session
Mettre à jour context_CW.md : état projet, décisions haut niveau, backlog — en déplaçant les chantiers terminés du backlog vers l'état projet
Supprimer toute information redondante entre les deux fichiers
Formuler le message de commit après lecture du diff, pas avant
Afficher les commandes à exécuter manuellement :

git add -A
git commit -m "<MESSAGE COMMIT>"
Contraintes

message commit qui résume sur une ligne sans signature

CLAUDE.md : détails d'implémentation uniquement
context_CW.md : rôles, état projet, décisions haut niveau, backlog
Rien de dupliqué entre les deux
Ne toucher à aucun autre fichier