import { Component } from '@angular/core';
import { StatRowComponent } from '../component/stat-row/stat-row.component';

@Component({
    selector: 'app-home',
    imports: [StatRowComponent],
    templateUrl: './home.component.html',
    styleUrl: './home.component.css'
})
export class HomeComponent {

}
