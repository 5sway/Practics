   M S B A M L   `   `   `  ����F  BHospitalApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:HospitalAppHospitalApp  5   	   O Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem 5   	       HospitalApp.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   +localclr-namespace:HospitalApp     5   	   6sys&clr-namespace:System;assembly=mscorlib     5   	     � Icon$  /Resources/logo.ico��5       � ShowInTaskbar .  � Title$ АИС "№20"��5	   	   $��Calibri)�6   $	��360��60   $	��750��6@   $	��500��6O   $
��1000��6_    � 	Resources 5
       �� 
TargetType �� Property �� Value   TransparentButton �� 
TargetType �� Triggers	 P� Property
 P� Value��%5  '��       &	  �     �� 5   
   " ��6   .�5      ��  � ��$ 14(�6)   �� 5       � �� �#3336+   �� 5       � ��$ Center=�62   5      �� 5   
   " ��6   .�5      ��  � ��$ 12(�6)   �� 5       � ��
 �����6+   �� 5       � ��
 ���� 6+   �� 5       � ��$ 0q�60   �� 5       � ��$ 5q�6(   �� 5       � ��$ 5q�6'   �� 5       � �� 5      �� 5      " ��6&   .R�5      �� #��z��6!   #��z��5   !   #��z��5   !   ._�5      �� $��Center=�6/   $��Center=�6L   5       5      P� 5       	 �@J�IsMouseOver
 . . �5!   "   ��  �@J�Opacity$	 0.8Q�6<   5"      P� 5#      	 � ��
 . . �5$   "   ��  �@J�Opacity$	 0.6Q�6<   5%      5'      5)      5*      5+      .��5,      �  � RowDefinitions 5-   
   �� 5.      $��44 �6   �� 5/      $��* �6   .+�51   
   � -   5        $��
HeaderGrid��51      $��0��6!   ���#6EDC82y�6C   �� *HeaderVisibility+.+�52      �� $+��%АИС Поликлиника "№20"��6   $��18(�65   $��	0,0,250,0q�6C   $��Center=�6V   �� 53      $��/Resources/logo.png��6   $��Center=�61   $
��Left=�6L   $��40��6g   $��5,0,0,0q�6r    K� ZIndex$ 10��6�   �� 54      -   5        $��ProfileBorder��54       �� CornerRadius$ 12��6*   $��Center=�6<   $��Right=�6W   $��0,0,10,0q�6s   ���#468C50$	��200��6�   $��35��6�   ._�55      � -   5        $��ProfileGrid��55      .+�56      �� -   5        $��
ProfileBtn��56      #��[  6/   $��Center=�6Z   $
��Left=�6u   $��30��6�   $��30��6�   $��5,0,0,0q�6�   .��57      �� $
��Fill=�6C   ��6    �� *UserProfileIcon+ J� Clip 58      @� 59   "    @� Center$ 15,15&�62    @� RadiusX$ 15Q�6A    @� RadiusY$ 15Q�6N   5;      5<      �� 5=      
�������$��12(�6Q   $��45,0,0,0q�6_   $
��Left=�6q   ��6    �� *UserFullName+6q   �� 5>      -   5        $��ExitProfileBtn��5>      #��[  63   $��
Выйти��6^   $��45��6n   $��25��6y   $��Right=�6�   $��0,0,10,0q�6�   6�   5?      5@      5A       � 5B   
   -   5        $��	MainFrame��5B        � NavigationUIVisibility$ Hidden=�6!   $��1��6A   6N   5C      5D      